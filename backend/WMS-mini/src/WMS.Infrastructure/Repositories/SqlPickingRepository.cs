using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlPickingRepository : IPickingRepository
{
    private readonly WmsDbContext _db;

    public SqlPickingRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Picking>> GetAllAsync()
    {
        return await _db.Pickings
            .Include(p => p.Warehouse)
            .Include(p => p.AssignedTo)
            .Include(p => p.PickingDetails)
                .ThenInclude(d => d.Product)
            .Include(p => p.PickingDetails)
                .ThenInclude(d => d.Location)
            .ToListAsync();
    }

    public async Task<Picking?> GetByIdAsync(Guid id)
    {
        return await _db.Pickings
            .Include(p => p.Warehouse)
            .Include(p => p.AssignedTo)
            .Include(p => p.PickingDetails)
                .ThenInclude(d => d.Product)
            .Include(p => p.PickingDetails)
                .ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Picking picking)
    {
        await _db.Pickings.AddAsync(picking);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Picking picking)
    {
        _db.Pickings.Update(picking);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Picking picking)
    {
        _db.Pickings.Remove(picking);
        await _db.SaveChangesAsync();
    }
}
