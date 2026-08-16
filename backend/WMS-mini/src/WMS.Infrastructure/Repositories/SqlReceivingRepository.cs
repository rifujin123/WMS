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

    public async Task<ReceivingDetail?> GetDetailByIdAsync(Guid id)
    {
        return await _db.ReceivingDetails
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(Receiving receiving)
    {
        await _db.Receivings.AddAsync(receiving);
    }

    public async Task UpdateAsync(Receiving receiving)
    {
        _db.Receivings.Update(receiving);
    }

    public async Task DeleteAsync(Receiving receiving)
    {
        // Mark details as deleted too because the required FK uses NoAction.
        _db.ReceivingDetails.RemoveRange(receiving.ReceivingDetails);
        _db.Receivings.Remove(receiving);
    }
}
