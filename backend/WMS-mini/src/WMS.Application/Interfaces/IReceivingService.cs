using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IReceivingService
{
    Task<List<ReceivingDto>> GetAllAsync();
    Task<ReceivingDto?> GetByIdAsync(Guid id);
    Task<ReceivingDto?> CreateAsync(CreateReceivingDto dto);
    Task<ReceivingDto?> ConfirmAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
