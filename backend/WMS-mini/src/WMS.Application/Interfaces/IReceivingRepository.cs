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
    Task<DiscrepancyReport?> GetDiscrepancyReportByReceivingIdAsync(Guid receivingId);
    Task AddDiscrepancyReportAsync(DiscrepancyReport report);
    Task UpdateDiscrepancyReportAsync(DiscrepancyReport report);
    Task DeleteDiscrepancyReportAsync(DiscrepancyReport report);
    Task AddAsync(Receiving receiving);
    Task UpdateAsync(Receiving receiving);
    Task RemoveDetailsAsync(Guid receivingId);
    Task DeleteAsync(Receiving receiving);
}
