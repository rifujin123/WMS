using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IReceivingRepository
{
    Task<List<Receiving>> GetAllAsync();
    Task<Receiving?> GetByIdAsync(Guid id);
    Task<ReceivingDetail?> GetDetailByIdAsync(Guid id);
    Task AddAsync(Receiving receiving);
    Task UpdateAsync(Receiving receiving);
    Task DeleteAsync(Receiving receiving);
}
