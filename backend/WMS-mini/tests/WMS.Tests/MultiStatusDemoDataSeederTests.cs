using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.Tests;

/// <summary>
/// Tests độc lập cho MultiStatusDemoDataSeeder:
/// Xác thực bộ seed đa trạng thái hoạt động chính xác trên SQLite in-memory seam contract,
/// bảo đảm bao phủ 100% các trạng thái nghiệp vụ Inbound, Outbound, Kiểm kê và Vận đơn.
/// </summary>
public class MultiStatusDemoDataSeederTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public void Dispose() => _connection.Dispose();

    private WmsDbContext CreateDb(string dbName)
    {
        _ = dbName;
        _connection.Open();
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(_connection)
            .Options;
        var db = new WmsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static MultiStatusDemoDataSeeder CreateSeeder(WmsDbContext db, bool enabled = true)
    {
        var options = Options.Create(new DemoSeedOptions { Enabled = enabled });
        return new MultiStatusDemoDataSeeder(db, options, NullLogger<MultiStatusDemoDataSeeder>.Instance);
    }

    [Fact]
    public async Task SeedAsync_WhenDisabled_ReturnsSkipped_AndDoesNotWrite()
    {
        await using var db = CreateDb(nameof(SeedAsync_WhenDisabled_ReturnsSkipped_AndDoesNotWrite));
        var seeder = CreateSeeder(db, enabled: false);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.True(summary.Skipped);
        Assert.False(summary.AlreadySeeded);
        Assert.Equal(0, await db.Warehouses.CountAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WhenDataAlreadyExists_SkipsToAvoidDuplicates()
    {
        await using var db = CreateDb(nameof(SeedAsync_WhenDataAlreadyExists_SkipsToAvoidDuplicates));
        db.Warehouses.Add(new Warehouse { Code = "WH-HCM", Name = "Kho HCM" });
        await db.SaveChangesAsync();
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.False(summary.Skipped);
        Assert.True(summary.AlreadySeeded);
        Assert.Equal(1, await db.Warehouses.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_CreatesUsers_WithManagerAndStaffRoles()
    {
        await using var db = CreateDb(nameof(SeedAsync_CreatesUsers_WithManagerAndStaffRoles));
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.False(summary.AlreadySeeded);
        Assert.Equal(6, summary.Users);
        Assert.Equal(6, await db.Users.CountAsync());

        var managerRoleId = await db.Roles.Where(r => r.Name == "WarehouseManager").Select(r => r.Id).FirstAsync();
        var staffRoleId = await db.Roles.Where(r => r.Name == "WarehouseStaff").Select(r => r.Id).FirstAsync();

        Assert.Equal(2, await db.UserRoles.CountAsync(ur => ur.RoleId == managerRoleId));
        Assert.Equal(4, await db.UserRoles.CountAsync(ur => ur.RoleId == staffRoleId));
    }

    [Fact]
    public async Task SeedAsync_CreatesWarehousesLocationsAndProducts()
    {
        await using var db = CreateDb(nameof(SeedAsync_CreatesWarehousesLocationsAndProducts));
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(2, summary.Warehouses);
        Assert.Equal(64, summary.Locations);
        Assert.Equal(4, summary.Categories);
        Assert.Equal(60, summary.Products);
        Assert.Equal(10, summary.Vendors);
        Assert.Equal(18, summary.Customers);
    }

    [Fact]
    public async Task SeedAsync_SeedsPoChains_HasAllFourStatuses()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsPoChains_HasAllFourStatuses));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var pos = await db.PurchaseOrders.AsNoTracking().ToListAsync();
        Assert.NotEmpty(pos);

        var statuses = pos.Select(p => p.Status).Distinct().ToList();
        Assert.Contains(PurchaseOrderStatus.Pending, statuses);
        Assert.Contains(PurchaseOrderStatus.Approved, statuses);
        Assert.Contains(PurchaseOrderStatus.Received, statuses);
        Assert.Contains(PurchaseOrderStatus.Closed, statuses);

        // Pending PO chưa có ApprovedDate
        var pendingPo = pos.First(p => p.Status == PurchaseOrderStatus.Pending);
        Assert.Null(pendingPo.ApprovedDate);
        Assert.Null(pendingPo.ApprovedById);

        // Approved / Received / Closed PO có ApprovedDate
        var approvedPo = pos.First(p => p.Status == PurchaseOrderStatus.Approved);
        Assert.NotNull(approvedPo.ApprovedDate);
        Assert.NotNull(approvedPo.ApprovedById);
    }

    [Fact]
    public async Task SeedAsync_SeedsReceivings_HasDraftAndConfirmed()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsReceivings_HasDraftAndConfirmed));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var receivings = await db.Receivings.Include(r => r.ReceivingDetails).AsNoTracking().ToListAsync();
        Assert.NotEmpty(receivings);

        var statuses = receivings.Select(r => r.Status).Distinct().ToList();
        Assert.Contains(ReceivingStatus.Draft, statuses);
        Assert.Contains(ReceivingStatus.Confirmed, statuses);

        // Draft receiving có cả Ok và Damaged detail để demo kiểm đếm
        var draft = receivings.First(r => r.Status == ReceivingStatus.Draft);
        Assert.Null(draft.ConfirmedDate);
        Assert.Contains(draft.ReceivingDetails, d => d.Condition == ProductCondition.Ok);
        Assert.Contains(draft.ReceivingDetails, d => d.Condition == ProductCondition.Damaged);
    }

    [Fact]
    public async Task SeedAsync_SeedsPutAwayTasks_HasAllFourStatuses()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsPutAwayTasks_HasAllFourStatuses));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var tasks = await db.PutAwayTasks.AsNoTracking().ToListAsync();
        Assert.NotEmpty(tasks);

        var statuses = tasks.Select(t => t.Status).Distinct().ToList();
        Assert.Contains(PutAwayTaskStatus.Open, statuses);
        Assert.Contains(PutAwayTaskStatus.Assigned, statuses);
        Assert.Contains(PutAwayTaskStatus.InProgress, statuses);
        Assert.Contains(PutAwayTaskStatus.Completed, statuses);

        // Open task chưa có AssignToId
        var openTask = tasks.First(t => t.Status == PutAwayTaskStatus.Open);
        Assert.Null(openTask.AssignToId);

        // Assigned task đã có AssignToId
        var assignedTask = tasks.First(t => t.Status == PutAwayTaskStatus.Assigned);
        Assert.NotNull(assignedTask.AssignToId);

        // InProgress task đã có StartedDate
        var inProgressTask = tasks.First(t => t.Status == PutAwayTaskStatus.InProgress);
        Assert.NotNull(inProgressTask.StartedDate);
    }

    [Fact]
    public async Task SeedAsync_SeedsSaleOrders_HasAllFiveStatuses()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsSaleOrders_HasAllFiveStatuses));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var saleOrders = await db.SaleOrders.Include(s => s.SaleOrderDetails).AsNoTracking().ToListAsync();
        Assert.NotEmpty(saleOrders);

        var statuses = saleOrders.Select(s => s.Status).Distinct().ToList();
        Assert.Contains(SaleOrderStatus.New, statuses);
        Assert.Contains(SaleOrderStatus.Allocated, statuses);
        Assert.Contains(SaleOrderStatus.Picking, statuses);
        Assert.Contains(SaleOrderStatus.Packed, statuses);
        Assert.Contains(SaleOrderStatus.Shipped, statuses);

        // New SO có AllocatedQty == 0
        var newOrder = saleOrders.First(s => s.Status == SaleOrderStatus.New);
        Assert.All(newOrder.SaleOrderDetails, d => Assert.Equal(0, d.AllocatedQty));

        // Allocated SO có AllocatedQty == Quantity
        var allocatedOrder = saleOrders.First(s => s.Status == SaleOrderStatus.Allocated);
        Assert.All(allocatedOrder.SaleOrderDetails, d => Assert.Equal(d.Quantity, d.AllocatedQty));
    }

    [Fact]
    public async Task SeedAsync_SeedsPickings_HasAllFourStatuses()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsPickings_HasAllFourStatuses));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var pickings = await db.Pickings.Include(p => p.PickingDetails).AsNoTracking().ToListAsync();
        Assert.NotEmpty(pickings);

        var statuses = pickings.Select(p => p.Status).Distinct().ToList();
        Assert.Contains(PickingStatus.Open, statuses);
        Assert.Contains(PickingStatus.Assigned, statuses);
        Assert.Contains(PickingStatus.InProgress, statuses);
        Assert.Contains(PickingStatus.Completed, statuses);

        // Open picking chưa có AssignedToId
        var openPk = pickings.First(p => p.Status == PickingStatus.Open);
        Assert.Null(openPk.AssignedToId);

        // InProgress picking có StartedDate
        var inProgressPk = pickings.First(p => p.Status == PickingStatus.InProgress);
        Assert.NotNull(inProgressPk.StartedDate);
    }

    [Fact]
    public async Task SeedAsync_SeedsShipments_ForShippedSaleOrders()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsShipments_ForShippedSaleOrders));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var shipments = await db.Shipments.Include(s => s.SaleOrder).AsNoTracking().ToListAsync();
        Assert.NotEmpty(shipments);
        Assert.True(shipments.Count >= 2);

        Assert.All(shipments, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Carrier));
            Assert.False(string.IsNullOrWhiteSpace(s.TrackingNo));
            Assert.NotNull(s.ShippedDate);
            Assert.Equal(SaleOrderStatus.Shipped, s.SaleOrder.Status);
        });
    }

    [Fact]
    public async Task SeedAsync_SeedsStockAdjustments_HasDraftAndApproved()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsStockAdjustments_HasDraftAndApproved));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var adjustments = await db.StockAdjustments.Include(a => a.Details).AsNoTracking().ToListAsync();
        Assert.NotEmpty(adjustments);

        var statuses = adjustments.Select(a => a.Status).Distinct().ToList();
        Assert.Contains(StockAdjustmentStatus.Draft, statuses);
        Assert.Contains(StockAdjustmentStatus.Approved, statuses);

        // Draft adjustment chưa có ApprovedDate
        var draft = adjustments.First(a => a.Status == StockAdjustmentStatus.Draft);
        Assert.Null(draft.ApprovedDate);
        Assert.Null(draft.ApprovedById);

        // Approved adjustment có ApprovedDate
        var approved = adjustments.First(a => a.Status == StockAdjustmentStatus.Approved);
        Assert.NotNull(approved.ApprovedDate);
        Assert.NotNull(approved.ApprovedById);
    }

    [Fact]
    public async Task SeedAsync_StockLedger_StaysConsistent_AndNeverNegative()
    {
        await using var db = CreateDb(nameof(SeedAsync_StockLedger_StaysConsistent_AndNeverNegative));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var stocks = await db.Stocks.AsNoTracking().ToListAsync();
        Assert.NotEmpty(stocks);

        foreach (var stock in stocks)
        {
            var movements = await db.StockMovements.AsNoTracking()
                .Where(m => m.ProductId == stock.ProductId && m.LocationId == stock.LocationId)
                .ToListAsync();

            var ledgerSum = movements.Sum(m => m.MovementType switch
            {
                MovementType.In => m.Qty,
                MovementType.Out => -m.Qty,
                MovementType.Adjustment => m.Qty,
                _ => 0
            });

            Assert.Equal(ledgerSum, stock.OnhandQty);
            Assert.True(stock.OnhandQty >= 0, $"Negative onhand for product {stock.ProductId}");
            Assert.True(stock.ReservedQty >= 0, $"Negative reserved for product {stock.ProductId}");
        }

        // Kiểm tra Location CurrentQuantity khớp chính xác tổng Onhand của location và <= MaxQuantity
        var locations = await db.Locations.AsNoTracking().ToListAsync();
        foreach (var loc in locations)
        {
            var totalOnhandAtLoc = stocks.Where(s => s.LocationId == loc.Id).Sum(s => s.OnhandQty);
            Assert.Equal(loc.CurrentQuantity, totalOnhandAtLoc);
            Assert.True(loc.CurrentQuantity <= loc.MaxQuantity,
                $"Location {loc.Code} over capacity: {loc.CurrentQuantity} > {loc.MaxQuantity}");
        }
    }

    [Fact]
    public async Task SeedAsync_ProducesComprehensiveStatusHistories()
    {
        await using var db = CreateDb(nameof(SeedAsync_ProducesComprehensiveStatusHistories));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var histories = await db.StatusHistories.AsNoTracking().ToListAsync();
        Assert.NotEmpty(histories);

        var entityTypes = histories.Select(h => h.EntityType).Distinct().ToList();
        Assert.Contains(nameof(PurchaseOrder), entityTypes);
        Assert.Contains(nameof(Receiving), entityTypes);
        Assert.Contains(nameof(PutAwayTask), entityTypes);
        Assert.Contains(nameof(SaleOrder), entityTypes);
        Assert.Contains(nameof(Picking), entityTypes);
        Assert.Contains(nameof(StockAdjustment), entityTypes);
    }
}
