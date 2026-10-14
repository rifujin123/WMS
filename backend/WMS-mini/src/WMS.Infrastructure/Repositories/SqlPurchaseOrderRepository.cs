using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
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
            .Include(p => p.Warehouse)
            .Include(p => p.PurchaseOrderDetails)
            .ThenInclude(d => d.Product)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(
        PurchaseOrderListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var orders = _db.PurchaseOrders.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            orders = orders.Where(po => po.PoNumber.Contains(search) || (po.VendorName != null && po.VendorName.Contains(search)));
        if (query.Status.HasValue)
            orders = orders.Where(po => po.Status == query.Status.Value);
        if (query.WarehouseId.HasValue)
            orders = orders.Where(po => po.WarehouseId == query.WarehouseId.Value);
        if (query.CreatedById.HasValue)
            orders = orders.Where(po => po.CreatedById == query.CreatedById.Value);

        var totalCount = await orders.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await orders
            .OrderByDescending(po => po.CreatedDate)
            .ThenBy(po => po.PoNumber)
            .ThenBy(po => po.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new PurchaseOrderDto
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                VendorId = po.VendorId,
                VendorName = po.VendorName,
                TotalAmount = po.TotalAmount,
                CreditOffsetAmount = po.CreditOffsetAmount,
                Status = po.Status,
                WarehouseId = po.WarehouseId,
                WarehouseName = po.Warehouse != null ? po.Warehouse.Name : null,
                WarehouseCode = po.Warehouse != null ? po.Warehouse.Code : null,
                ApprovedById = po.ApprovedById,
                ApprovedDate = po.ApprovedDate,
                CreatedById = po.CreatedById,
                CreatedDate = po.CreatedDate,
                PurchaseOrderDetails = po.PurchaseOrderDetails.Select(d => new PurchaseOrderDetailDto
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    ProductSku = d.Product.Sku,
                    ProductName = d.Product.Name,
                    OrderedQuantity = d.OrderedQuantity,
                    ReceivedQuantity = d.ReceivedQuantity,
                    UnitPrice = d.UnitPrice,
                    TaxRate = d.TaxRate,
                }).ToList(),
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PurchaseOrderDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<PurchaseOrder?> GetByIdAsync(Guid id)
    {
        return await _db.PurchaseOrders
            .Include(po => po.Warehouse)
            .Include(po => po.PurchaseOrderDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    public async Task<PurchaseOrderDetail?> GetDetailByIdAsync(Guid detailId)
    {
        return await _db.PurchaseOrderDetails
            .Include(d => d.PurchaseOrder)
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == detailId);
    }

    public Task<bool> ExistsByPoNumberAsync(string poNumber) =>
        _db.PurchaseOrders.AnyAsync(po => po.PoNumber == poNumber);

    public async Task AddAsync(PurchaseOrder purchaseOrder)
    {
        await _db.PurchaseOrders.AddAsync(purchaseOrder);
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _db.PurchaseOrders.Update(purchaseOrder);
    }

    public async Task DeleteAsync(PurchaseOrder purchaseOrder)
    {
        var detailIds = purchaseOrder.PurchaseOrderDetails.Select(d => d.Id).ToList();
        if (detailIds.Count > 0)
        {
            var links = await _db.BackorderLinks.Where(b => detailIds.Contains(b.PurchaseOrderDetailId)).ToListAsync();
            if (links.Count > 0)
            {
                _db.BackorderLinks.RemoveRange(links);
            }
        }

        _db.PurchaseOrders.Remove(purchaseOrder);
    }

    public async Task RemoveDetailsAsync(Guid purchaseOrderId)
    {
        var details = await _db.PurchaseOrderDetails
            .Where(d => d.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();

        var detailIds = details.Select(d => d.Id).ToList();
        if (detailIds.Count > 0)
        {
            var links = await _db.BackorderLinks.Where(b => detailIds.Contains(b.PurchaseOrderDetailId)).ToListAsync();
            if (links.Count > 0)
            {
                _db.BackorderLinks.RemoveRange(links);
            }
        }

        _db.PurchaseOrderDetails.RemoveRange(details);
    }
}
