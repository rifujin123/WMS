using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IPickingService
{
    Task<List<PickingDto>> GetAllAsync();
    Task<PickingDto?> GetByIdAsync(Guid id);
    Task<PickingDto> CreateAsync(CreatePickingDto dto, Guid userId);
    Task<PickingDto?> AssignAsync(Guid id, Guid userId);
    Task<PickingDto?> StartProgressAsync(Guid id);
    Task<PickingDto?> CompleteAsync(Guid id, CompletePickingDto dto);
    Task<bool> DeleteAsync(Guid id);
}
