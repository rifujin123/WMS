using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IVendorDebtRepository? _vendorDebtRepo;

    public PurchaseOrderService(
        IPurchaseOrderRepository repo,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IVendorDebtRepository? vendorDebtRepo = null)
    {
        _repo = repo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _vendorDebtRepo = vendorDebtRepo;
    }

    public async Task<List<PurchaseOrderDto>> GetAllAsync()
    {
        var orders = await _repo.GetAllAsync();
        if (!_currentUser.IsInRole("Admin") && _currentUser.WarehouseId.HasValue)
        {
            orders = orders.Where(p => p.WarehouseId == _currentUser.WarehouseId.Value).ToList();
        }
        return _mapper.Map<List<PurchaseOrderDto>>(orders);
    }

    public Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(
        PurchaseOrderListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsInRole("Admin") && _currentUser.WarehouseId.HasValue)
        {
            query.WarehouseId = _currentUser.WarehouseId.Value;
        }
        return _repo.GetPagedAsync(query, pageSize, cancellationToken);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null)
            return null;
        var dto = _mapper.Map<PurchaseOrderDto>(entity);
        if (_vendorDebtRepo != null)
        {
            var resolvedDebts = await _vendorDebtRepo.GetByResolvedInPoIdAsync(id);
            dto.ResolvedDebtIds = resolvedDebts.Select(d => d.Id).ToList();
        }
        return dto;
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        if (await _repo.ExistsByPoNumberAsync(dto.PoNumber))
            throw new InvalidOperationException($"Số PO '{dto.PoNumber}' đã tồn tại.");
        var entity = _mapper.Map<PurchaseOrder>(dto);
        if (!entity.WarehouseId.HasValue && _currentUser.WarehouseId.HasValue)
        {
            entity.WarehouseId = _currentUser.WarehouseId.Value;
        }
        if (entity.PurchaseOrderDetails.Any(d => d.UnitPrice < 0 || d.TaxRate < 0))
            throw new InvalidOperationException("Đơn giá và thuế suất không được nhỏ hơn 0.");

        entity.Status = PurchaseOrderStatus.Pending;
        var rawTotal = Math.Round(
            entity.PurchaseOrderDetails.Sum(d => d.OrderedQuantity * d.UnitPrice * (1 + d.TaxRate / 100m)),
            2,
            MidpointRounding.AwayFromZero);

        if (dto.CreditOffsetAmount < 0)
            throw new InvalidOperationException("Số tiền giảm trừ công nợ không được nhỏ hơn 0.");

        if (dto.CreditOffsetAmount > rawTotal)
            throw new InvalidOperationException("Số tiền giảm trừ công nợ không được vượt quá tổng giá trị đơn hàng.");

        entity.CreditOffsetAmount = dto.CreditOffsetAmount;
        entity.TotalAmount = Math.Max(0m, rawTotal - dto.CreditOffsetAmount);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repo.AddAsync(entity);

            if (dto.ResolvedDebtIds != null && dto.ResolvedDebtIds.Count > 0 && _vendorDebtRepo != null)
            {
                var debts = await _vendorDebtRepo.GetByIdsAsync(dto.ResolvedDebtIds);
                foreach (var debt in debts)
                {
                    if (debt.IsResolved || (entity.VendorId.HasValue && debt.VendorId.HasValue && debt.VendorId.Value != entity.VendorId.Value))
                    {
                        throw new InvalidOperationException("Khoản nợ không hợp lệ, đã giải quyết hoặc không thuộc nhà cung cấp này.");
                    }
                    debt.IsResolved = true;
                    debt.ResolvedInPoId = entity.Id;
                    debt.ResolvedDate = DateTime.UtcNow;
                    await _vendorDebtRepo.UpdateAsync(debt);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        });

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<PurchaseOrderDto?> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.Status != PurchaseOrderStatus.Pending) return null;
        entity.VendorId = dto.VendorId;
        entity.VendorName = dto.VendorName;
        await _repo.RemoveDetailsAsync(entity.Id);
        entity.PurchaseOrderDetails = _mapper.Map<List<PurchaseOrderDetail>>(dto.PurchaseOrderDetails);
        foreach (var detail in entity.PurchaseOrderDetails) detail.PurchaseOrderId = entity.Id;
        if (entity.PurchaseOrderDetails.Any(d => d.UnitPrice < 0 || d.TaxRate < 0))
            throw new InvalidOperationException("Đơn giá và thuế suất không được nhỏ hơn 0.");

        entity.CreditOffsetAmount = dto.CreditOffsetAmount;
        var rawTotal = Math.Round(
            entity.PurchaseOrderDetails.Sum(d => d.OrderedQuantity * d.UnitPrice * (1 + d.TaxRate / 100m)),
            2,
            MidpointRounding.AwayFromZero);
        entity.TotalAmount = Math.Max(0m, rawTotal - entity.CreditOffsetAmount);

        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.Status != PurchaseOrderStatus.Pending) return false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (_vendorDebtRepo != null)
            {
                var resolvedDebts = await _vendorDebtRepo.GetByResolvedInPoIdAsync(id);
                foreach (var debt in resolvedDebts)
                {
                    debt.IsResolved = false;
                    debt.ResolvedInPoId = null;
                    debt.ResolvedDate = null;
                    await _vendorDebtRepo.UpdateAsync(debt);
                }
            }

            await _repo.RemoveDetailsAsync(id);
            await _repo.DeleteAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        });

        return true;
    }

    public async Task<PurchaseOrderDto?> ApproveAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.Status != PurchaseOrderStatus.Pending) return null;
        entity.Status = PurchaseOrderStatus.Approved;
        entity.ApprovedById = _currentUser.UserId;
        entity.ApprovedDate = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(entity);
    }

    public async Task<PurchaseOrderDto?> CloseAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || (entity.Status != PurchaseOrderStatus.Received && entity.Status != PurchaseOrderStatus.PartiallyReceived)) return null;
        entity.Status = PurchaseOrderStatus.Closed;
        entity.ClosedById = _currentUser.UserId;
        entity.ClosedDate = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(entity);
    }
}


