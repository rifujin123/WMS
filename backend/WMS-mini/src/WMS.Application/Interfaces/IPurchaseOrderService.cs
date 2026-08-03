using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderDto>> GetAllAsync();
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto, Guid userId);
    Task<PurchaseOrderDto?> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<PurchaseOrderDto?> ApproveAsync(Guid id);
    Task<PurchaseOrderDto?> CloseAsync(Guid id);
}
