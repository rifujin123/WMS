using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IStockService
{
    Task<List<StockDto>> GetAllAsync();
    Task<PagedResult<StockSummaryDto>> GetSummaryPagedAsync(StockSummaryQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<StockDto?> GetByIdAsync(Guid id);
    Task<List<StockDto>> GetByProductAsync(Guid productId);
    Task<List<StockDto>> GetByLocationAsync(Guid locationId);
}