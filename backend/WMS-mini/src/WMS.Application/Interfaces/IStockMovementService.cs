using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IStockMovementService
{
    Task<PagedResult<StockMovementDto>> GetPagedAsync(StockMovementQueryDto query, int pageSize, CancellationToken cancellationToken = default);
}
