using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IStockMovementService
{
    Task<List<StockMovementDto>> GetAsync(StockMovementQueryDto query);
}
