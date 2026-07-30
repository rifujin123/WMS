using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlStockRepository : IStockRepository
{
    private readonly WmsDbContext _db;

    public SqlStockRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Stock>> GetAllAsync()
    {
        return await _db.Stocks.ToListAsync();
    }

    public async Task<Stock?> GetByIdAsync(Guid id)
    {
        return await _db.Stocks.FindAsync(id);
    }

    public async Task<Stock?> GetByProductAndLocationAsync(Guid productId, Guid locationId)
    {
        return await _db.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locationId);
    }

    public async Task AddAsync(Stock stock)
    {
        await _db.Stocks.AddAsync(stock);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Stock stock)
    {
        _db.Stocks.Update(stock);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Stock stock)
    {
        _db.Stocks.Remove(stock);
        await _db.SaveChangesAsync();
    }
}
