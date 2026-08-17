using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IStockMovementRepository
{
    Task<List<StockMovement>> GetAllAsync();
    Task<StockMovement?> GetByIdAsync(Guid id);
    Task<List<StockMovement>> GetAsync(StockMovementQueryDto query);
    Task AddAsync(StockMovement stockMovement);
    Task UpdateAsync(StockMovement stockMovement);
    Task DeleteAsync(StockMovement stockMovement);
}
