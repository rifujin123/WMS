using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IStockService
{
    Task<List<StockDto>> GetAllAsync();
    Task<StockDto?> GetByIdAsync(Guid id);
    Task<List<StockDto>> GetByProductAsync(Guid productId);
}