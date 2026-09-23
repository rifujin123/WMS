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

    public PurchaseOrderService(IPurchaseOrderRepository repo, IMapper mapper, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _repo = repo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
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
        return _mapper.Map<PurchaseOrderDto>(entity);
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
        entity.Status = PurchaseOrderStatus.Pending;
        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<PurchaseOrderDto?> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.Status != PurchaseOrderStatus.Pending) return null;
        entity.VendorName = dto.VendorName;
        await _repo.RemoveDetailsAsync(entity.Id);
        entity.PurchaseOrderDetails = _mapper.Map<List<PurchaseOrderDetail>>(dto.PurchaseOrderDetails);
        foreach (var detail in entity.PurchaseOrderDetails) detail.PurchaseOrderId = entity.Id;
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null || entity.Status != PurchaseOrderStatus.Pending) return false;
        await _repo.RemoveDetailsAsync(id);
        await _repo.DeleteAsync(entity);
        await _unitOfWork.SaveChangesAsync();
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
        if (entity == null || entity.Status != PurchaseOrderStatus.Received) return null;
        entity.Status = PurchaseOrderStatus.Closed;
        entity.ClosedById = _currentUser.UserId;
        entity.ClosedDate = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(entity);
    }
}


