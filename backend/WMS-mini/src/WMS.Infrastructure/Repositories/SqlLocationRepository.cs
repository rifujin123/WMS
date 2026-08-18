using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlLocationRepository : ILocationRepository
{
    private readonly WmsDbContext _db;

    public SqlLocationRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Location>> GetAllAsync()
    {
        return await _db.Locations.ToListAsync();
    }

    public async Task<Location?> GetByIdAsync(Guid id)
    {
        return await _db.Locations.FindAsync(id);
    }

    public async Task<List<Location>> GetByWarehouseIdAsync(Guid warehouseId)
    {
        return await _db.Locations
            .Where(l => l.WarehouseId == warehouseId)
            .ToListAsync();
    }

    public async Task AddAsync(Location location)
    {
        await _db.Locations.AddAsync(location);
    }

    public async Task UpdateAsync(Location location)
    {
        _db.Locations.Update(location);
    }

    public async Task DeleteAsync(Location location)
    {
        _db.Locations.Remove(location);
    }

    public async Task<bool> HasStockAsync(Guid locationId)
    {
        return await _db.Stocks.AnyAsync(s => s.LocationId == locationId);
    }

    public async Task<Location?> GetByWarehouseAndCodeAsync(Guid warehouseId, string code)
    {
        return await _db.Locations.FirstOrDefaultAsync(l =>
            l.WarehouseId == warehouseId && l.Code == code);
    }
}
