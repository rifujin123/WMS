using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlPurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly WmsDbContext _db;

    public SqlPurchaseOrderRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<PurchaseOrder>> GetAllAsync()
    {
        return await _db.PurchaseOrders
            .Include(p => p.PurchaseOrderDetails)
            .ThenInclude(d => d.Product)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(Guid id)
    {
        return await _db.PurchaseOrders
            .Include(po => po.PurchaseOrderDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder)
    {
        await _db.PurchaseOrders.AddAsync(purchaseOrder);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _db.PurchaseOrders.Update(purchaseOrder);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(PurchaseOrder purchaseOrder)
    {
        _db.PurchaseOrders.Remove(purchaseOrder);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveDetailsAsync(Guid purchaseOrderId)
    {
        var details = await _db.PurchaseOrderDetails
            .Where(d => d.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();

        _db.PurchaseOrderDetails.RemoveRange(details);
        await _db.SaveChangesAsync();
    }
}
