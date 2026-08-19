using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IReceivingService
{
    Task<List<ReceivingDto>> GetAllAsync();
    Task<PagedResult<ReceivingDto>> GetPagedAsync(ReceivingListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<ReceivingDto?> GetByIdAsync(Guid id);
    Task<ReceivingDto> CreateAsync(CreateReceivingDto dto);
    Task<ReceivingDto?> ConfirmAsync(Guid id);
    Task<ReceivingDto?> UpdateAsync(Guid id, CreateReceivingDto dto);
    Task<bool> DeleteAsync(Guid id);
}
