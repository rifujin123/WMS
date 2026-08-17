using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services;

public class SaleOrderService : ISaleOrderService
{
    private readonly ISaleOrderRepository _repo;
    private readonly IProductRepository _productRepo;
    private readonly IPickingRepository _pickingRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public SaleOrderService(
        ISaleOrderRepository repo,
        IProductRepository productRepo,
        IPickingRepository pickingRepo,
        IStockRepository stockRepo,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _productRepo = productRepo;
        _pickingRepo = pickingRepo;
        _stockRepo = stockRepo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SaleOrderDto>> GetAllAsync()
    {
        var saleOrders = await _repo.GetAllAsync();
        return _mapper.Map<List<SaleOrderDto>>(saleOrders);
    }

    public async Task<SaleOrderDto?> GetByIdAsync(Guid id)
    {
        var saleOrder = await _repo.GetByIdAsync(id);
        if(saleOrder == null)
            return null;
        return _mapper.Map<SaleOrderDto>(saleOrder);
    }

    public async Task<SaleOrderDto> CreateAsync(CreateSaleOrderDto dto)
    {
        ValidateBusinessRules(dto);
        await ValidateProductsExistAsync(dto);

        var orderNo = dto.OrderNo.Trim();
        if (await _repo.GetByOrderNoAsync(orderNo) != null)
            throw new InvalidOperationException($"SaleOrder number '{orderNo}' already exists.");

        var createdDate = DateTime.UtcNow;
        var saleOrder = _mapper.Map<SaleOrder>(dto);
        saleOrder.OrderNo = orderNo;
        saleOrder.Status = SaleOrderStatus.New;
        saleOrder.CreatedDate = createdDate;

        foreach (var detail in saleOrder.SaleOrderDetails)
        {
            detail.AllocatedQty = 0;
            detail.Status = SaleOrderDetailStatus.Pending;
            detail.CreatedDate = createdDate;
        }

        await _repo.AddAsync(saleOrder);
        await _unitOfWork.SaveChangesAsync();
        return (await GetByIdAsync(saleOrder.Id))!;
    }

    public async Task<SaleOrderDto?> UpdateAsync(Guid id, CreateSaleOrderDto dto)
    {
        var saleOrder = await _repo.GetByIdAsync(id);
        if (saleOrder == null)
            return null;

        if (saleOrder.Status != SaleOrderStatus.New)
            throw new InvalidOperationException(
                $"Cannot update SaleOrder in '{saleOrder.Status}' status. Must be 'New'.");

        ValidateBusinessRules(dto);
        await ValidateProductsExistAsync(dto);

        var orderNo = dto.OrderNo.Trim();
        var duplicate = await _repo.GetByOrderNoAsync(orderNo);
        if (duplicate != null && duplicate.Id != id)
            throw new InvalidOperationException($"SaleOrder number '{orderNo}' already exists.");

        saleOrder.OrderNo = orderNo;
        saleOrder.CustomerName = dto.CustomerName;
        saleOrder.OrderDate = dto.OrderDate;

        await _repo.RemoveDetailsAsync(id);
        saleOrder.SaleOrderDetails = _mapper.Map<List<SaleOrderDetail>>(dto.SaleOrderDetails);
        foreach (var detail in saleOrder.SaleOrderDetails)
        {
            detail.SaleOrderId = id;
            detail.AllocatedQty = 0;
            detail.Status = SaleOrderDetailStatus.Pending;
            detail.CreatedDate = DateTime.UtcNow;
        }

        await _repo.UpdateAsync(saleOrder);
        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var saleOrder = await _repo.GetByIdAsync(id);
        if (saleOrder == null)
            return false;

        if (saleOrder.Status != SaleOrderStatus.New)
            throw new InvalidOperationException(
                $"Cannot delete SaleOrder in '{saleOrder.Status}' status. Must be 'New'.");

        await _repo.DeleteAsync(saleOrder);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var saleOrder = await _repo.GetByIdAsync(id);
        if (saleOrder == null)
            return false;

        if (saleOrder.Status is not (SaleOrderStatus.New or SaleOrderStatus.Allocated))
            throw new InvalidOperationException(
                $"Cannot cancel SaleOrder in '{saleOrder.Status}' status. Must be 'New' or 'Allocated'.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (saleOrder.Status == SaleOrderStatus.Allocated)
            {
                var pickings = await _pickingRepo.GetOpenBySaleOrderIdAsync(id);
                foreach (var picking in pickings)
                {
                    foreach (var detail in picking.PickingDetails)
                    {
                        if (detail.LocationId != null)
                        {
                            var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId.Value);
                            if (stock != null)
                            {
                                stock.ReservedQty -= detail.QtyToPick;
                                await _stockRepo.UpdateAsync(stock);
                            }
                        }
                        if (detail.SaleOrderDetailId != null)
                        {
                            var sod = await _repo.GetDetailByIdAsync(detail.SaleOrderDetailId.Value);
                            if (sod != null)
                            {
                                sod.AllocatedQty -= detail.QtyToPick;
                                sod.Status = SaleOrderDetailStatus.Pending;
                            }
                        }
                    }
                    await _pickingRepo.DeleteAsync(picking);
                }
            }

            saleOrder.Status = SaleOrderStatus.Cancelled;
            await _repo.UpdateAsync(saleOrder);
            await _unitOfWork.SaveChangesAsync();
        });

        return true;
    }

    private static void ValidateBusinessRules(CreateSaleOrderDto dto)
    {
        var productIds = dto.SaleOrderDetails
            .Select(d => d.ProductId)
            .ToList();

        if (productIds.Distinct().Count() != productIds.Count)
            throw new InvalidOperationException(
                "SaleOrder details must not contain duplicate products.");
    }

    private async Task ValidateProductsExistAsync(CreateSaleOrderDto dto)
    {
        var productIds = dto.SaleOrderDetails
            .Select(d => d.ProductId)
            .Distinct()
            .ToList();

        var existingProductIds = await _productRepo.GetExistingIdsAsync(productIds);
        var missingProductId = productIds
            .Except(existingProductIds)
            .FirstOrDefault();

        if (missingProductId != Guid.Empty)
            throw new InvalidOperationException(
                $"Product '{missingProductId}' not found.");
    }
}
