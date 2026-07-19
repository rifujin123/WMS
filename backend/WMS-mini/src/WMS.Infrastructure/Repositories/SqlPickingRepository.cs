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
        return await _db.Pickings.ToListAsync();
    }

    public async Task<Picking?> GetByIdAsync(Guid id)
    {
        return await _db.Pickings.FindAsync(id);
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
