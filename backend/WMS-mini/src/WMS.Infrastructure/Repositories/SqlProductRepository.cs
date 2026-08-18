using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlProductRepository : IProductRepository
{
    private readonly WmsDbContext _db;

    public SqlProductRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _db.Products.ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _db.Products.FindAsync(id);
    }

    public async Task<List<Guid>> GetExistingIdsAsync(List<Guid> productIds)
    {
        return await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _db.Products.AddAsync(product);
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
    }

    public async Task DeleteAsync(Product product)
    {
        _db.Products.Remove(product);
    }

    public async Task<bool> HasReferencesAsync(Guid productId)
    {
        return await _db.Stocks.AnyAsync(s => s.ProductId == productId)
            || await _db.PurchaseOrderDetails.AnyAsync(d => d.ProductId == productId)
            || await _db.StockMovements.AnyAsync(m => m.ProductId == productId)
            || await _db.SaleOrderDetails.AnyAsync(d => d.ProductId == productId)
            || await _db.PickingDetails.AnyAsync(d => d.ProductId == productId)
            || await _db.ReceivingDetails.AnyAsync(d => d.ProductId == productId)
            || await _db.PutAwayTasks.AnyAsync(t => t.ProductId == productId)
            || await _db.RmaDetails.AnyAsync(d => d.ProductId == productId);
    }
}
