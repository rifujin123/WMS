using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IStockAdjustmentRepository
{
    Task<List<StockAdjustment>> GetAllAsync();
    Task<StockAdjustment?> GetByIdAsync(Guid id);
    Task AddAsync(StockAdjustment adjustment);
    Task UpdateAsync(StockAdjustment adjustment);
    Task DeleteAsync(StockAdjustment adjustment);
}