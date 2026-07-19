using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlWarehouseRepository : IWarehouseRepository
{
    private readonly WmsDbContext _db;

    public SqlWarehouseRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Warehouse>> GetAllAsync()
    {
        return await _db.Warehouses.ToListAsync();
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id)
    {
        return await _db.Warehouses.FindAsync(id);
    }

    public async Task<bool> HasLocationsAsync(Guid warehouseId)
    {
        return await _db.Locations.AnyAsync(l => l.WarehouseId == warehouseId);
    }

    public async Task AddAsync(Warehouse warehouse)
    {
        await _db.Warehouses.AddAsync(warehouse);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Warehouse warehouse)
    {
        _db.Warehouses.Update(warehouse);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Warehouse warehouse)
    {
        _db.Warehouses.Remove(warehouse);
        await _db.SaveChangesAsync();
    }
}
