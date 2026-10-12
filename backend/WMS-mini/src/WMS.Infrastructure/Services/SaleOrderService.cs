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
    private readonly ICustomerRepository _customerRepo;
    private readonly IPickingService _pickingService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SaleOrderService(
        ISaleOrderRepository repo,
        IProductRepository productRepo,
        ICustomerRepository customerRepo,
        IPickingService pickingService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _productRepo = productRepo;
        _customerRepo = customerRepo;
        _pickingService = pickingService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<List<SaleOrderDto>> GetAllAsync()
    {
        var saleOrders = await _repo.GetAllAsync();
        if (!_currentUser.IsInRole("Admin") && _currentUser.WarehouseId.HasValue)
        {
            saleOrders = saleOrders.Where(s => s.WarehouseId == _currentUser.WarehouseId.Value).ToList();
        }
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

        if (!dto.WarehouseId.HasValue && _currentUser.WarehouseId.HasValue)
        {
            dto.WarehouseId = _currentUser.WarehouseId.Value;
        }

        var orderNo = dto.OrderNo.Trim();
        if (await _repo.GetByOrderNoAsync(orderNo) != null)
            throw new InvalidOperationException($"Số đơn bán hàng '{orderNo}' đã tồn tại trong hệ thống.");

        string? customerName = dto.CustomerName;
        if (dto.CustomerId.HasValue)
        {
            var customer = await _customerRepo.GetByIdAsync(dto.CustomerId.Value);
            if (customer == null)
                throw new InvalidOperationException("Khách hàng không tồn tại trong hệ thống.");
            customerName = customer.Name;
        }

        var createdDate = DateTime.UtcNow;
        var saleOrder = _mapper.Map<SaleOrder>(dto);
        saleOrder.OrderNo = orderNo;
        saleOrder.CustomerId = dto.CustomerId;
        saleOrder.CustomerName = customerName;
        saleOrder.TotalAmount = dto.SaleOrderDetails.Sum(d => d.Quantity * d.UnitPrice);
        saleOrder.CreatedDate = createdDate;

        if (dto.IsPreOrder)
        {
            saleOrder.IsPreOrder = true;
            saleOrder.RequiredDeposit = dto.RequiredDeposit;
            saleOrder.PaidDeposit = dto.PaidDeposit;
            saleOrder.DepositDeadline = dto.DepositDeadline;
            saleOrder.DepositNotes = dto.DepositNotes;
            saleOrder.Status = dto.PaidDeposit >= dto.RequiredDeposit
                ? SaleOrderStatus.Backorder
                : SaleOrderStatus.AwaitingDeposit;
        }
        else
        {
            saleOrder.IsPreOrder = false;
            saleOrder.RequiredDeposit = 0;
            saleOrder.PaidDeposit = 0;
            saleOrder.DepositDeadline = null;
            saleOrder.DepositNotes = null;
            saleOrder.Status = SaleOrderStatus.New;
        }

        foreach (var detail in saleOrder.SaleOrderDetails)
        {
            detail.AllocatedQty = 0;
            detail.Status = SaleOrderDetailStatus.Pending;
            detail.CreatedDate = createdDate;
        }

        await _repo.AddAsync(saleOrder);
        await _unitOfWork.SaveChangesAsync();

        if (!dto.IsPreOrder && dto.WarehouseId.HasValue && dto.WarehouseId.Value != Guid.Empty)
        {
            try
            {
                await _pickingService.CreateAsync(new CreatePickingDto
                {
                    SaleOrderId = saleOrder.Id,
                    WarehouseId = dto.WarehouseId.Value
                });
            }
            catch
            {
                // Nếu tạo picking task thất bại (ví dụ: thiếu tồn kho khả dụng), rollback đơn bán vừa tạo
                await _repo.DeleteAsync(saleOrder);
                await _unitOfWork.SaveChangesAsync();
                throw;
            }
        }

        return (await GetByIdAsync(saleOrder.Id))!;
    }

    public async Task<SaleOrderDto?> UpdateAsync(Guid id, CreateSaleOrderDto dto)
    {
        var saleOrder = await _repo.GetByIdAsync(id);
        if (saleOrder == null)
            return null;

        if (saleOrder.Status != SaleOrderStatus.New)
            throw new InvalidOperationException(
                $"Không thể cập nhật đơn bán ở trạng thái '{saleOrder.Status}'. Đơn bán phải ở trạng thái 'Mới' (New).");

        ValidateBusinessRules(dto);
        await ValidateProductsExistAsync(dto);

        var orderNo = dto.OrderNo.Trim();
        var duplicate = await _repo.GetByOrderNoAsync(orderNo);
        if (duplicate != null && duplicate.Id != id)
            throw new InvalidOperationException($"Số đơn bán hàng '{orderNo}' đã tồn tại trong hệ thống.");

        string? customerName = dto.CustomerName;
        if (dto.CustomerId.HasValue)
        {
            var customer = await _customerRepo.GetByIdAsync(dto.CustomerId.Value);
            if (customer == null)
                throw new InvalidOperationException("Khách hàng không tồn tại trong hệ thống.");
            customerName = customer.Name;
        }

        saleOrder.OrderNo = orderNo;
        saleOrder.CustomerId = dto.CustomerId;
        saleOrder.CustomerName = customerName;
        saleOrder.OrderDate = dto.OrderDate;
        saleOrder.TotalAmount = dto.SaleOrderDetails.Sum(d => d.Quantity * d.UnitPrice);

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

    public async Task<SaleOrderDto?> ConfirmDepositAsync(Guid id, ConfirmDepositDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.PaidDeposit <= 0)
            throw new InvalidOperationException("Số tiền cọc thực nhận phải lớn hơn 0.");

        var saleOrder = await _repo.GetByIdAsync(id);
        if (saleOrder == null)
            return null;

        if (saleOrder.Status != SaleOrderStatus.AwaitingDeposit)
            throw new InvalidOperationException(
                $"Chỉ có thể xác nhận cọc cho đơn ở trạng thái 'Chờ cọc' (AwaitingDeposit). Trạng thái hiện tại: '{saleOrder.Status}'.");

        saleOrder.PaidDeposit += dto.PaidDeposit;
        if (!string.IsNullOrWhiteSpace(dto.DepositNotes))
        {
            saleOrder.DepositNotes = dto.DepositNotes;
        }

        if (saleOrder.PaidDeposit >= saleOrder.RequiredDeposit)
        {
            saleOrder.Status = SaleOrderStatus.Backorder;
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
                $"Không thể xóa đơn bán ở trạng thái '{saleOrder.Status}'. Đơn bán phải ở trạng thái 'Mới' (New).");

        await _repo.DeleteAsync(saleOrder);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static void ValidateBusinessRules(CreateSaleOrderDto dto)
    {
        if (dto.IsPreOrder)
        {
            if (dto.RequiredDeposit <= 0)
                throw new InvalidOperationException("Tiền cọc yêu cầu phải lớn hơn 0 đối với đơn đặt trước.");
            if (dto.PaidDeposit < 0)
                throw new InvalidOperationException("Số tiền cọc đã nhận không được âm.");
        }

        if (dto.SaleOrderDetails.Any(d => d.UnitPrice < 0))
            throw new InvalidOperationException("Đơn giá sản phẩm không được âm.");

        var productIds = dto.SaleOrderDetails
            .Select(d => d.ProductId)
            .ToList();

        if (productIds.Distinct().Count() != productIds.Count)
            throw new InvalidOperationException(
                "Chi tiết đơn bán không được chứa các sản phẩm trùng lặp.");
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
                $"Không tìm thấy sản phẩm '{missingProductId}'.");
    }
}
