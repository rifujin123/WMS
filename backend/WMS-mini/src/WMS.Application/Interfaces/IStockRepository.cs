using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IStockRepository
{
    Task<List<Stock>> GetAllAsync();
    Task<Stock?> GetByIdAsync(Guid id);
    Task<Stock?> GetByProductAndLocationAsync(Guid productId, Guid locationId);
    Task AddAsync(Stock stock);
    Task UpdateAsync(Stock stock);
    Task DeleteAsync(Stock stock);
}
