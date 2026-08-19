using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface ILocationService
{
    Task<List<LocationDto>> GetAllAsync();
    Task<PagedResult<LocationDto>> GetPagedAsync(LocationListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<LocationDto?> GetByIdAsync(Guid id);
    Task<List<LocationDto>> GetByWarehouseAsync(Guid warehouseId);
    Task<LocationDto> CreateAsync(CreateLocationDto dto);
    Task<LocationDto?> UpdateAsync(Guid id, UpdateLocationDto dto);
    Task<bool> DeleteAsync(Guid id);
}
