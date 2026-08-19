using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
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

    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        ProductListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var products = _db.Products.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            products = products.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        var totalCount = await products.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                CategoryId = p.CategoryId,
                Unit = p.Unit,
                Price = p.Price,
                Dimension = p.Dimension,
                ImageUrl = p.ImageUrl,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<ProductDto>.Create(items, page, pageSize, totalCount);
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
