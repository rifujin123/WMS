using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
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

    public async Task<PagedResult<WarehouseDto>> GetPagedAsync(
        WarehouseListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var warehouses = _db.Warehouses.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            warehouses = warehouses.Where(w => w.Name.Contains(search) || w.Code.Contains(search));
        }

        var totalCount = await warehouses.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await warehouses
            .OrderBy(w => w.Name)
            .ThenBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name,
                Address = w.Address,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<WarehouseDto>.Create(items, page, pageSize, totalCount);
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
    }

    public async Task UpdateAsync(Warehouse warehouse)
    {
        _db.Warehouses.Update(warehouse);
    }

    public async Task DeleteAsync(Warehouse warehouse)
    {
        _db.Warehouses.Remove(warehouse);
    }
}
