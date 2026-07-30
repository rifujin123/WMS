using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IPutAwayService
{
    Task<List<PutAwayTaskDto>> GetAllAsync();
    Task<PutAwayTaskDto?> GetByIdAsync(Guid id);
    Task<PutAwayTaskDto> CreateAsync(CreatePutAwayTaskDto dto);
    Task<PutAwayTaskDto?> UpdateAsync(Guid id, UpdatePutAwayTaskDto dto);
    Task<bool> DeleteAsync(Guid id);

    // Business operations
    Task<PutAwayTaskDto?> AssignAsync(Guid id, Guid userId);
    Task<PutAwayTaskDto?> StartProgressAsync(Guid id);
    Task<PutAwayTaskDto?> CompleteAsync(Guid id);
}