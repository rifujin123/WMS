using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IWarehouseRepository
{
    Task<List<Warehouse>> GetAllAsync();
    Task<PagedResult<WarehouseDto>> GetPagedAsync(WarehouseListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByIdAsync(Guid id);
    Task<bool> HasLocationsAsync(Guid warehouseId);
    Task AddAsync(Warehouse warehouse);
    Task UpdateAsync(Warehouse warehouse);
    Task DeleteAsync(Warehouse warehouse);
}
