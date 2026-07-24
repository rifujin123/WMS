using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlReceivingRepository : IReceivingRepository
{
    private readonly WmsDbContext _db;

    public SqlReceivingRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Receiving>> GetAllAsync()
    {
        return await _db.Receivings
            .Include(r => r.PurchaseOrder)
            .Include(r => r.ReceivingDetails)
                .ThenInclude(d => d.Product)
            .ToListAsync();
    }

    public async Task<Receiving?> GetByIdAsync(Guid id)
    {
        return await _db.Receivings
            .Include(r => r.PurchaseOrder)
            .Include(r => r.ReceivingDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(Receiving receiving)
    {
        await _db.Receivings.AddAsync(receiving);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Receiving receiving)
    {
        _db.Receivings.Update(receiving);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Receiving receiving)
    {
        _db.Receivings.Remove(receiving);
        await _db.SaveChangesAsync();
    }
}
