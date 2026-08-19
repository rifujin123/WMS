using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IReceivingRepository
{
    Task<List<Receiving>> GetAllAsync();
    Task<PagedResult<ReceivingDto>> GetPagedAsync(ReceivingListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<Receiving?> GetByIdAsync(Guid id);
    Task<Receiving?> GetConfirmedByPurchaseOrderIdAsync(Guid purchaseOrderId);
    Task<ReceivingDetail?> GetDetailByIdAsync(Guid id);
    Task AddAsync(Receiving receiving);
    Task UpdateAsync(Receiving receiving);
    Task DeleteAsync(Receiving receiving);
}
