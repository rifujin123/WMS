using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IStockRepository
{
    Task<List<Stock>> GetAllAsync();
    Task<Stock?> GetByIdAsync(Guid id);
    Task<List<Stock>> GetByProductAsync(Guid productId);
    Task<List<Stock>> GetAvailableByProductAndWarehouseAsync(Guid productId, Guid warehouseId);
    Task<Stock?> GetByProductAndLocationAsync(Guid productId, Guid locationId);
    Task<List<Stock>> GetByLocationAsync(Guid locationId);
    Task AddAsync(Stock stock);
    Task UpdateAsync(Stock stock);
    Task DeleteAsync(Stock stock);
}
