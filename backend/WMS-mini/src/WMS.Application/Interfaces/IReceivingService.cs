using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IReceivingService
{
    Task<List<ReceivingDto>> GetAllAsync();
    Task<ReceivingDto?> GetByIdAsync(Guid id);
    Task<ReceivingDto> CreateAsync(CreateReceivingDto dto, Guid userId);
    Task<ReceivingDto?> ConfirmAsync(Guid id);
    Task<ReceivingDto?> UpdateAsync(Guid id, CreateReceivingDto dto, Guid userId);
    Task<bool> DeleteAsync(Guid id);
}