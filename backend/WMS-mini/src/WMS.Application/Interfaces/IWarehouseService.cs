using WMS.Application.DTOs;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IWarehouseService
{
    Task<List<WarehouseDto>> GetAllAsync();
    Task<PagedResult<WarehouseDto>> GetPagedAsync(WarehouseListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetByIdAsync(Guid id);
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto);
    Task<WarehouseDto?> UpdateAsync(Guid id, UpdateWarehouseDto dto);
    Task<DeleteWarehouseResult> DeleteAsync(Guid id);
}
