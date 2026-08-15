using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlStockMovementRepository : IStockMovementRepository
{
    private readonly WmsDbContext _db;

    public SqlStockMovementRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<StockMovement>> GetAllAsync()
    {
        return await _db.StockMovements.ToListAsync();
    }

    public async Task<StockMovement?> GetByIdAsync(Guid id)
    {
        return await _db.StockMovements.FindAsync(id);
    }

    public async Task AddAsync(StockMovement stockMovement)
    {
        await _db.StockMovements.AddAsync(stockMovement);
    }

    public async Task UpdateAsync(StockMovement stockMovement)
    {
        _db.StockMovements.Update(stockMovement);
    }

    public async Task DeleteAsync(StockMovement stockMovement)
    {
        _db.StockMovements.Remove(stockMovement);
    }
}
