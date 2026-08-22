using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IStockMovementService
{
    Task<PagedResult<StockMovementDto>> GetAsync(StockMovementQueryDto query, int pageSize, CancellationToken cancellationToken = default);
}
