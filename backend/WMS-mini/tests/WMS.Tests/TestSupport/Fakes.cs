using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Application.Mappings;
using WMS.Domain.Entities;

namespace WMS.Tests.TestSupport;

/// Fake ICurrentUserService — chỉ test logic nghiệp vụ, không test auth.
public class FakeCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; }
    public string? UserName { get; set; }
    public bool IsAuthenticated => UserId != null;
    public bool AdminOrManager { get; set; } = true;

    public FakeCurrentUser(Guid? userId = null) => UserId = userId;

    public bool IsInRole(params string[] roles)
    {
        if (AdminOrManager && (roles.Contains("Admin") || roles.Contains("WarehouseManager"))) return true;
        return roles.Contains("WarehouseStaff") && UserId != null;
    }
}

/// Fake IUnitOfWork — transaction chỉ chạy action, SaveChanges no-op.
public class FakeUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Commits++;
        return Task.FromResult(1);
    }

    public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        => action();
}

public static class MapperFactory
{
    public static IMapper Create()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile(new MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        return config.CreateMapper();
    }
}

// ---- Repository fakes: giữ entity trong List để thao tác service thay đổi trực tiếp ----

public class FakePurchaseOrderRepository : IPurchaseOrderRepository
{
    public List<PurchaseOrder> Items { get; } = new();
    public Task<List<PurchaseOrder>> GetAllAsync() => Task.FromResult(Items);
    public Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(PurchaseOrderListQuery query, int pageSize, CancellationToken ct = default)
        => Task.FromResult(PagedResult<PurchaseOrderDto>.Create(new List<PurchaseOrderDto>(), query.Page, pageSize, 0));
    public Task<PurchaseOrder?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<bool> ExistsByPoNumberAsync(string poNumber) => Task.FromResult(Items.Any(x => x.PoNumber == poNumber));
    public Task AddAsync(PurchaseOrder po) { Items.Add(po); return Task.CompletedTask; }
    public Task UpdateAsync(PurchaseOrder po) => Task.CompletedTask;
    public Task DeleteAsync(PurchaseOrder po) { Items.Remove(po); return Task.CompletedTask; }
    public Task RemoveDetailsAsync(Guid purchaseOrderId) => Task.CompletedTask;
}

public class FakeReceivingRepository : IReceivingRepository
{
    public List<Receiving> Items { get; } = new();
    public Task<List<Receiving>> GetAllAsync() => Task.FromResult(Items);
    public Task<PagedResult<ReceivingDto>> GetPagedAsync(ReceivingListQuery query, int pageSize, CancellationToken ct = default)
        => Task.FromResult(PagedResult<ReceivingDto>.Create(new List<ReceivingDto>(), query.Page, pageSize, 0));
    public Task<Receiving?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<Receiving?> GetConfirmedByPurchaseOrderIdAsync(Guid purchaseOrderId)
        => Task.FromResult(Items.FirstOrDefault(x => x.PurchaseOrderId == purchaseOrderId && x.Status == Domain.Enums.ReceivingStatus.Confirmed));
    public Task<ReceivingDetail?> GetDetailByIdAsync(Guid id)
    {
        var receiving = Items.FirstOrDefault(r => r.ReceivingDetails.Any(d => d.Id == id));
        return Task.FromResult(receiving?.ReceivingDetails.FirstOrDefault(d => d.Id == id));
    }
    public Task AddAsync(Receiving receiving) { Items.Add(receiving); return Task.CompletedTask; }
    public Task UpdateAsync(Receiving receiving) => Task.CompletedTask;
    public Task RemoveDetailsAsync(Guid receivingId)
    {
        var receiving = Items.FirstOrDefault(r => r.Id == receivingId);
        receiving?.ReceivingDetails.Clear();
        return Task.CompletedTask;
    }
    public Task DeleteAsync(Receiving receiving) { Items.Remove(receiving); return Task.CompletedTask; }
}

public class FakePutAwayTaskRepository : IPutAwayTaskRepository
{
    public List<PutAwayTask> Items { get; } = new();
    public Task<List<PutAwayTask>> GetAllAsync(Guid? assignToId = null)
        => Task.FromResult(assignToId == null ? Items : Items.Where(t => t.AssignToId == assignToId).ToList());
    public Task<PagedResult<PutAwayTaskDto>> GetPagedAsync(PutAwayTaskListQuery query, int pageSize, CancellationToken ct = default)
        => Task.FromResult(PagedResult<PutAwayTaskDto>.Create(new List<PutAwayTaskDto>(), query.Page, pageSize, 0));
    public Task<PutAwayTask?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(t => t.Id == id));
    public Task AddAsync(PutAwayTask task) { Items.Add(task); return Task.CompletedTask; }
    public Task UpdateAsync(PutAwayTask task) => Task.CompletedTask;
    public Task DeleteAsync(PutAwayTask task) { Items.Remove(task); return Task.CompletedTask; }
    public Task<int> GetIncompleteCountByPurchaseOrderAsync(Guid purchaseOrderId)
        => Task.FromResult(Items.Count(t =>
            t.ReceivingDetail?.Receiving?.PurchaseOrderId == purchaseOrderId &&
            t.Status != Domain.Enums.PutAwayTaskStatus.Completed));
}

public class FakeStockRepository : IStockRepository
{
    public List<Stock> Items { get; } = new();
    public Task<List<Stock>> GetAllAsync() => Task.FromResult(Items);
    public Task<PagedResult<StockSummaryDto>> GetSummaryPagedAsync(StockSummaryQuery query, int pageSize, CancellationToken ct = default)
        => Task.FromResult(PagedResult<StockSummaryDto>.Create(new List<StockSummaryDto>(), query.Page, pageSize, 0));
    public Task<Stock?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<List<Stock>> GetByProductAsync(Guid productId) => Task.FromResult(Items.Where(s => s.ProductId == productId).ToList());
    public Task<List<Stock>> GetAvailableByProductAndWarehouseAsync(Guid productId, Guid warehouseId)
        => Task.FromResult(Items.Where(s => s.ProductId == productId && s.Location?.WarehouseId == warehouseId).ToList());
    public Task<Stock?> GetByProductAndLocationAsync(Guid productId, Guid locationId)
        => Task.FromResult(Items.FirstOrDefault(s => s.ProductId == productId && s.LocationId == locationId));
    public Task<List<Stock>> GetByLocationAsync(Guid locationId) => Task.FromResult(Items.Where(s => s.LocationId == locationId).ToList());
    public Task AddAsync(Stock stock) { Items.Add(stock); return Task.CompletedTask; }
    public Task UpdateAsync(Stock stock) => Task.CompletedTask;
    public Task DeleteAsync(Stock stock) { Items.Remove(stock); return Task.CompletedTask; }
}

public class FakeStockMovementRepository : IStockMovementRepository
{
    public List<StockMovement> Items { get; } = new();
    public Task<List<StockMovement>> GetAllAsync() => Task.FromResult(Items);
    public Task<StockMovement?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(m => m.Id == id));
    public Task<PagedResult<StockMovement>> GetAsync(StockMovementQueryDto query, int pageSize, CancellationToken cancellationToken = default)
    {
        var filtered = Items
            .Where(m =>
                (!query.ProductId.HasValue || m.ProductId == query.ProductId.Value) &&
                (!query.LocationId.HasValue || m.LocationId == query.LocationId.Value) &&
                (!query.MovementType.HasValue || m.MovementType == query.MovementType.Value))
            .OrderByDescending(m => m.CreatedDate)
            .ToList();
        var pageItems = filtered.Skip((Math.Max(query.Page, 1) - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(PagedResult<StockMovement>.Create(pageItems, Math.Max(query.Page, 1), pageSize, filtered.Count));
    }
    public Task AddAsync(StockMovement movement) { Items.Add(movement); return Task.CompletedTask; }
    public Task UpdateAsync(StockMovement movement) => Task.CompletedTask;
    public Task DeleteAsync(StockMovement movement) { Items.Remove(movement); return Task.CompletedTask; }
}

public class FakeLocationRepository : ILocationRepository
{
    public List<Location> Items { get; } = new();
    public Task<List<Location>> GetAllAsync() => Task.FromResult(Items);
    public Task<PagedResult<LocationDto>> GetPagedAsync(LocationListQuery query, int pageSize, CancellationToken ct = default)
        => Task.FromResult(PagedResult<LocationDto>.Create(new List<LocationDto>(), query.Page, pageSize, 0));
    public Task<Location?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(l => l.Id == id));
    public Task<List<Location>> GetByWarehouseIdAsync(Guid warehouseId) => Task.FromResult(Items.Where(l => l.WarehouseId == warehouseId).ToList());
    public Task AddAsync(Location location) { Items.Add(location); return Task.CompletedTask; }
    public Task UpdateAsync(Location location) => Task.CompletedTask;
    public Task DeleteAsync(Location location) { Items.Remove(location); return Task.CompletedTask; }
    public Task<bool> HasStockAsync(Guid locationId) => Task.FromResult(false);
    public Task<Location?> GetByWarehouseAndCodeAsync(Guid warehouseId, string code)
        => Task.FromResult(Items.FirstOrDefault(l => l.WarehouseId == warehouseId && l.Code == code));
}

public class FakeWarehouseRepository : IWarehouseRepository
{
    public List<Warehouse> Items { get; } = new();
    public Task<List<Warehouse>> GetAllAsync() => Task.FromResult(Items);
    public Task<PagedResult<WarehouseDto>> GetPagedAsync(WarehouseListQuery query, int pageSize, CancellationToken ct = default)
        => Task.FromResult(PagedResult<WarehouseDto>.Create(new List<WarehouseDto>(), query.Page, pageSize, 0));
    public Task<Warehouse?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(w => w.Id == id));
    public Task<bool> HasLocationsAsync(Guid warehouseId) => Task.FromResult(Items.Any(w => w.Id == warehouseId));
    public Task AddAsync(Warehouse warehouse) { Items.Add(warehouse); return Task.CompletedTask; }
    public Task UpdateAsync(Warehouse warehouse) => Task.CompletedTask;
    public Task DeleteAsync(Warehouse warehouse) { Items.Remove(warehouse); return Task.CompletedTask; }
}

public class FakePickingRepository : IPickingRepository
{
    public List<Picking> Items { get; } = new();
    public Task<List<Picking>> GetAllAsync(Guid? assignToId = null)
        => Task.FromResult(assignToId == null ? Items : Items.Where(p => p.AssignedToId == assignToId).ToList());
    public Task<Picking?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(p => p.Id == id));
    public Task AddAsync(Picking picking) { Items.Add(picking); return Task.CompletedTask; }
    public Task UpdateAsync(Picking picking) => Task.CompletedTask;
    public Task DeleteAsync(Picking picking) { Items.Remove(picking); return Task.CompletedTask; }
    public Task<List<Guid>> GetPickingIdsExceptAsync(Guid excludeId) => Task.FromResult(Items.Where(p => p.Id != excludeId).Select(p => p.Id).ToList());
}

public class FakeSaleOrderRepository : ISaleOrderRepository
{
    public List<SaleOrder> Items { get; } = new();
    public Task<List<SaleOrder>> GetAllAsync() => Task.FromResult(Items);
    public Task<SaleOrder?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(o => o.Id == id));
    public Task<SaleOrder?> GetByOrderNoAsync(string orderNo) => Task.FromResult(Items.FirstOrDefault(o => o.OrderNo == orderNo));
    public Task AddAsync(SaleOrder saleOrder) { Items.Add(saleOrder); return Task.CompletedTask; }
    public Task UpdateAsync(SaleOrder saleOrder) => Task.CompletedTask;
    public Task DeleteAsync(SaleOrder saleOrder) { Items.Remove(saleOrder); return Task.CompletedTask; }
    public Task RemoveDetailsAsync(Guid saleOrderId) => Task.CompletedTask;
    public Task<SaleOrderDetail?> GetDetailByIdAsync(Guid detailId)
        => Task.FromResult(Items.SelectMany(o => o.SaleOrderDetails).FirstOrDefault(d => d.Id == detailId));
    public Task<List<SaleOrderDetail>> GetDetailsWithOrdersByIdsAsync(List<Guid> detailIds)
    {
        var result = new List<SaleOrderDetail>();
        foreach (var order in Items)
        {
            foreach (var detail in order.SaleOrderDetails.Where(d => detailIds.Contains(d.Id)))
            {
                if (detail.SaleOrder == null)
                {
                    detail.SaleOrder = order;
                }
                result.Add(detail);
            }
        }
        return Task.FromResult(result);
    }
    public Task<List<Guid>> GetSaleOrderIdsByPickingsAsync(List<Guid> pickingIds)
        => Task.FromResult(Items.Where(o => o.SaleOrderDetails.Any(d => d.PickingDetails.Any(p => pickingIds.Contains(p.PickingId)))).Select(o => o.Id).ToList());
    public Task<List<SaleOrder>> GetByIdsAsync(List<Guid> ids) => Task.FromResult(Items.Where(o => ids.Contains(o.Id)).ToList());
}