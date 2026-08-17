using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IPickingService
{
    Task<List<PickingDto>> GetAllAsync(Guid? assignToId = null);
    Task<PickingDto?> GetByIdAsync(Guid id);
    Task<PickingDto> CreateAsync(CreatePickingDto dto);
    Task<PickingDto?> AssignAsync(Guid id, Guid assignedToId);
    Task<PickingDto?> StartProgressAsync(Guid id);
    Task<PickingDto?> CompleteAsync(Guid id, CompletePickingDto dto);
    Task<bool> DeleteAsync(Guid id);
}
