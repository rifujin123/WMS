using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlStockRepository : IStockRepository
{
    private readonly WmsDbContext _db;

    public SqlStockRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Stock>> GetAllAsync()
    {
        return await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Location)
            .ToListAsync();
    }

    public async Task<PagedResult<StockSummaryDto>> GetSummaryPagedAsync(
        StockSummaryQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var stocks = _db.Stocks
            .AsNoTracking()
            .AsQueryable();

        if (query.LocationId.HasValue)
        {
            var productIdsAtLocation = _db.Stocks
                .Where(s => s.LocationId == query.LocationId.Value)
                .Select(s => s.ProductId);
            stocks = stocks.Where(s => productIdsAtLocation.Contains(s.ProductId));
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            stocks = stocks.Where(s => s.Product.Name.Contains(search) || s.Product.Sku.Contains(search));
        }

        var grouped = stocks
            .GroupBy(s => new { s.ProductId, s.Product.Sku, s.Product.Name });
        var totalCount = await grouped.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await grouped
            .OrderBy(g => g.Key.Sku)
            .ThenBy(g => g.Key.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new StockSummaryDto
            {
                ProductId = g.Key.ProductId,
                ProductSku = g.Key.Sku,
                ProductName = g.Key.Name,
                TotalOnhand = g.Sum(s => s.OnhandQty),
                TotalReserved = g.Sum(s => s.ReservedQty),
                LocationCount = g.Select(s => s.LocationId).Distinct().Count(),
            })
            .ToListAsync(cancellationToken);

        return PagedResult<StockSummaryDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<Stock?> GetByIdAsync(Guid id)
    {
        return await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Location)
                .ThenInclude(l => l.Warehouse)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Stock>> GetByProductAsync(Guid productId)
    {
        return await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Location)
                .ThenInclude(l => l.Warehouse)
            .Where(s => s.ProductId == productId)
            .ToListAsync();
    }

    public async Task<List<Stock>> GetAvailableByProductAndWarehouseAsync(Guid productId, Guid warehouseId)
    {
        var tenantId = _db.CurrentTenantId;
        var hasExpiry = false;
        if (tenantId.HasValue)
        {
            hasExpiry = await _db.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Id == tenantId.Value)
                .Select(t => t.HasExpiryManagement)
                .FirstOrDefaultAsync();
        }

        var query = _db.Stocks
            .Include(s => s.Location)
            .Where(s => s.ProductId == productId
                && s.Location.WarehouseId == warehouseId
                && s.OnhandQty - s.ReservedQty > 0);

        if (hasExpiry)
        {
            // FEFO: Hết hạn trước xuất trước. Loại trừ các lô đã hết hạn.
            var now = DateTime.UtcNow;
            return await query
                .Where(s => s.ExpiryDate == null || s.ExpiryDate > now)
                .OrderBy(s => s.ExpiryDate == null ? 1 : 0)
                .ThenBy(s => s.ExpiryDate)
                .ThenBy(s => s.Location.Code)
                .ToListAsync();
        }

        // FIFO: Nhập trước xuất trước theo ngày tạo
        return await query
            .OrderBy(s => s.CreatedDate)
            .ThenBy(s => s.Location.Code)
            .ToListAsync();
    }

    public async Task<Stock?> GetByProductAndLocationAsync(Guid productId, Guid locationId, string? lotNumber = null, DateTime? expiryDate = null)
    {
        return await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.ProductId == productId
                && s.LocationId == locationId
                && s.LotNumber == lotNumber
                && s.ExpiryDate == expiryDate);
    }

    public async Task AddAsync(Stock stock)
    {
        await _db.Stocks.AddAsync(stock);
    }

    public async Task UpdateAsync(Stock stock)
    {
        _db.Stocks.Update(stock);
    }

    public async Task DeleteAsync(Stock stock)
    {
        _db.Stocks.Remove(stock);
    }

    public async Task<List<Stock>> GetByLocationAsync(Guid locationId){
        return await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Location)
            .Where(s => s.LocationId == locationId)
            .ToListAsync();
    }
}
