using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlShipmentRepository : IShipmentRepository
{
    private readonly WmsDbContext _db;

    public SqlShipmentRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Shipment>> GetAllAsync()
    {
        return await _db.Shipments.ToListAsync();
    }

    public async Task<Shipment?> GetByIdAsync(Guid id)
    {
        return await _db.Shipments.FindAsync(id);
    }

    public async Task AddAsync(Shipment shipment)
    {
        await _db.Shipments.AddAsync(shipment);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Shipment shipment)
    {
        _db.Shipments.Update(shipment);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Shipment shipment)
    {
        _db.Shipments.Remove(shipment);
        await _db.SaveChangesAsync();
    }
}
