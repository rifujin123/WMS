using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface ILocationRepository
{
    Task<List<Location>> GetAllAsync();
    Task<Location?> GetByIdAsync(Guid id);
    Task<List<Location>> GetByWarehouseIdAsync(Guid warehouseId);
    Task AddAsync(Location location);
    Task UpdateAsync(Location location);
    Task DeleteAsync(Location location);
    Task<bool> HasStockAsync(Guid locationId);
}
