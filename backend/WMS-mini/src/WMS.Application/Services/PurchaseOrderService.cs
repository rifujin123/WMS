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

    public async Task<List<PurchaseOrderDto>> GetAllAsync() => _mapper.Map<List<PurchaseOrderDto>>(await _repo.GetAllAsync());

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if(entity == null)
            return null;
        return _mapper.Map<PurchaseOrderDto>(entity);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        var entity = _mapper.Map<PurchaseOrder>(dto);
        entity.Status = PurchaseOrderStatus.Pending;
        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(entity);
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
