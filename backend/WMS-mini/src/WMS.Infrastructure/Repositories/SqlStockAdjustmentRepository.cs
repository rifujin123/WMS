using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlStockAdjustmentRepository : IStockAdjustmentRepository
{
    private readonly WmsDbContext _db;

    public SqlStockAdjustmentRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<StockAdjustment>> GetAllAsync()
    {
        return await _db.StockAdjustments
            .Include(a => a.Details)
                .ThenInclude(d => d.Product)
            .Include(a => a.Details)
                .ThenInclude(d => d.Location)
            .ToListAsync();
    }

    public async Task<StockAdjustment?> GetByIdAsync(Guid id)
    {
        return await _db.StockAdjustments
            .Include(a => a.Details)
                .ThenInclude(d => d.Product)
            .Include(a => a.Details)
                .ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task AddAsync(StockAdjustment adjustment)
    {
        await _db.StockAdjustments.AddAsync(adjustment);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(StockAdjustment adjustment)
    {
        _db.StockAdjustments.Update(adjustment);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(StockAdjustment adjustment)
    {
        _db.StockAdjustments.Remove(adjustment);
        await _db.SaveChangesAsync();
    }
}