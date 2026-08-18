using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IPutAwayService
{
    Task<List<PutAwayTaskDto>> GetAllAsync(Guid? assignToId = null);
    Task<PutAwayTaskDto?> GetByIdAsync(Guid id);
    Task<PutAwayTaskDto> CreateAsync(CreatePutAwayTaskDto dto);
    Task<PutAwayTaskDto?> UpdateAsync(Guid id, UpdatePutAwayTaskDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<PutAwayTaskDto?> AssignAsync(Guid id, Guid assignedToId);
    Task<PutAwayTaskDto?> StartProgressAsync(Guid id);
    Task<PutAwayTaskDto?> CompleteAsync(Guid id);
}
