using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlBackorderLinkRepository : IBackorderLinkRepository
{
    private readonly WmsDbContext _db;

    public SqlBackorderLinkRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<BackorderLink?> GetByIdAsync(Guid id)
    {
        return await _db.BackorderLinks
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.SaleOrder)
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.Product)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.PurchaseOrder)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<BackorderLink>> GetAllAsync(Guid? saleOrderId = null, Guid? purchaseOrderId = null, Guid? productId = null)
    {
        var query = _db.BackorderLinks
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.SaleOrder)
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.Product)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.PurchaseOrder)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.Product)
            .AsQueryable();

        if (saleOrderId.HasValue)
            query = query.Where(b => b.SaleOrderDetail.SaleOrderId == saleOrderId.Value);

        if (purchaseOrderId.HasValue)
            query = query.Where(b => b.PurchaseOrderDetail.PurchaseOrderId == purchaseOrderId.Value);

        if (productId.HasValue)
            query = query.Where(b => b.SaleOrderDetail.ProductId == productId.Value);

        return await query.OrderByDescending(b => b.CreatedDate).ToListAsync();
    }

    public async Task<List<BackorderLink>> GetBySaleOrderDetailIdAsync(Guid saleOrderDetailId)
    {
        return await _db.BackorderLinks
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.SaleOrder)
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.Product)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.PurchaseOrder)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.Product)
            .Where(b => b.SaleOrderDetailId == saleOrderDetailId)
            .ToListAsync();
    }

    public async Task<List<BackorderLink>> GetBySaleOrderDetailIdsAsync(List<Guid> saleOrderDetailIds)
    {
        return await _db.BackorderLinks
            .Where(b => saleOrderDetailIds.Contains(b.SaleOrderDetailId))
            .ToListAsync();
    }

    public async Task<List<BackorderLink>> GetByPurchaseOrderDetailIdAsync(Guid purchaseOrderDetailId)
    {
        return await _db.BackorderLinks
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.SaleOrder)
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.Product)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.PurchaseOrder)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.Product)
            .Where(b => b.PurchaseOrderDetailId == purchaseOrderDetailId)
            .ToListAsync();
    }

    public async Task<List<BackorderLink>> GetByPurchaseOrderDetailIdsAsync(List<Guid> purchaseOrderDetailIds)
    {
        return await _db.BackorderLinks
            .Where(b => purchaseOrderDetailIds.Contains(b.PurchaseOrderDetailId))
            .ToListAsync();
    }

    public async Task<List<BackorderLink>> GetUnfulfilledByPurchaseOrderDetailIdAsync(Guid purchaseOrderDetailId)
    {
        return await _db.BackorderLinks
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.SaleOrder)
                    .ThenInclude(so => so.SaleOrderDetails)
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.Product)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.PurchaseOrder)
            .Where(b => b.PurchaseOrderDetailId == purchaseOrderDetailId && !b.IsFulfilled && b.Quantity > b.FulfilledQuantity)
            .OrderBy(b => b.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<BackorderLink>> GetUnfulfilledByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        return await _db.BackorderLinks
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.SaleOrder)
                    .ThenInclude(so => so.SaleOrderDetails)
            .Include(b => b.SaleOrderDetail)
                .ThenInclude(s => s.Product)
            .Include(b => b.PurchaseOrderDetail)
                .ThenInclude(p => p.PurchaseOrder)
            .Where(b => b.PurchaseOrderDetail.PurchaseOrderId == purchaseOrderId && !b.IsFulfilled && b.Quantity > b.FulfilledQuantity)
            .OrderBy(b => b.CreatedDate)
            .ToListAsync();
    }

    public async Task AddAsync(BackorderLink link)
    {
        await _db.BackorderLinks.AddAsync(link);
    }

    public async Task UpdateAsync(BackorderLink link)
    {
        _db.BackorderLinks.Update(link);
    }

    public async Task DeleteAsync(BackorderLink link)
    {
        _db.BackorderLinks.Remove(link);
    }

    public async Task DeleteRangeAsync(IEnumerable<BackorderLink> links)
    {
        _db.BackorderLinks.RemoveRange(links);
    }
}
