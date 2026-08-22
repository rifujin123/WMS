using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.Tests;

/// Tests cho demo-data seeder tại seam contract: chạy qua WmsDbContext SQLite in-memory.
/// Dùng SQLite thay InMemory vì ticket 06 backdate lịch sử qua raw SQL (ExecuteSqlRaw).
/// Ticket 01 — gating (Seed:Enabled) + base idempotency (warehouse đã tồn tại → skip).
/// Ticket 02 — seed users & roles (2 manager + 4 staff), idempotent theo username.
public class DemoDataSeederTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public void Dispose() => _connection.Dispose();

    private WmsDbContext CreateDb(string dbName)
    {
        _ = dbName; // tên DB giữ chữ ký tương thích; mỗi test cô lập bằng connection riêng
        _connection.Open();
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(_connection)
            .Options;
        var db = new WmsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static DemoDataSeeder CreateSeeder(WmsDbContext db, bool enabled = true)
    {
        var options = Options.Create(new DemoSeedOptions { Enabled = enabled });
        return new DemoDataSeeder(db, options, NullLogger<DemoDataSeeder>.Instance);
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
    public async Task SeedAsync_CreatesSixDemoUsers_WithCorrectRoles()
    {
        await using var db = CreateDb(nameof(SeedAsync_CreatesSixDemoUsers_WithCorrectRoles));
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.False(summary.AlreadySeeded);
        Assert.Equal(6, summary.Users);
        Assert.Equal(6, await db.Users.CountAsync());
        Assert.Equal(2, await CountUsersInRoleAsync(db, "WarehouseManager"));
        Assert.Equal(4, await CountUsersInRoleAsync(db, "WarehouseStaff"));
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateUsers()
    {
        await using var db = CreateDb(nameof(SeedAsync_RunTwice_DoesNotDuplicateUsers));
        var seeder = CreateSeeder(db);

        var first = await seeder.SeedAsync(CancellationToken.None);
        var second = await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(6, first.Users);
        Assert.Equal(6, await db.Users.CountAsync());
        // Lần chạy sau: user đã tồn tại → bỏ qua (không tạo kép)
        Assert.Equal(0, second.Users);
        Assert.Equal(6, await db.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_CreatesTwoWarehouses_WithExpectedLocationCounts()
    {
        await using var db = CreateDb(nameof(SeedAsync_CreatesTwoWarehouses_WithExpectedLocationCounts));
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(2, summary.Warehouses);
        Assert.Equal(64, summary.Locations);

        var hcm = await db.Warehouses.FirstAsync(w => w.Code == "WH-HCM");
        var hn = await db.Warehouses.FirstAsync(w => w.Code == "WH-HN");
        Assert.Equal(24, await db.Locations.CountAsync(l => l.WarehouseId == hcm.Id));
        Assert.Equal(40, await db.Locations.CountAsync(l => l.WarehouseId == hn.Id));
        Assert.Equal(64, await db.Locations.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WarehouseLocations_HaveUniqueCodesPerWarehouse_AndPositiveCapacity()
    {
        await using var db = CreateDb(nameof(SeedAsync_WarehouseLocations_HaveUniqueCodesPerWarehouse_AndPositiveCapacity));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var hcm = await db.Warehouses.FirstAsync(w => w.Code == "WH-HCM");
        var hn = await db.Warehouses.FirstAsync(w => w.Code == "WH-HN");

        foreach (var warehouseId in new[] { hcm.Id, hn.Id })
        {
            var codes = await db.Locations.Where(l => l.WarehouseId == warehouseId).Select(l => l.Code).ToListAsync();
            Assert.Equal(codes.Count, codes.Distinct().Count()); // unique trong kho
        }

        var allLocations = await db.Locations.ToListAsync();
        Assert.All(allLocations, l => Assert.True(l.MaxQuantity > 0));
        // CurrentQuantity do ticket 06 populate; phạm vi & capacity được test riêng ở
        // SeedAsync_LocationCurrentQuantity_RespectsCapacity.
        Assert.All(allLocations, l => Assert.True(l.CurrentQuantity >= 0));
    }

    [Fact]
    public async Task SeedAsync_WarehouseLocations_HaveOnlyKnownTypes_AndCodeFormat()
    {
        await using var db = CreateDb(nameof(SeedAsync_WarehouseLocations_HaveOnlyKnownTypes_AndCodeFormat));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var allLocations = await db.Locations.ToListAsync();
        var knownTypes = Enum.GetValues<LocationType>();
        Assert.All(allLocations, l => Assert.Contains(l.LocationType, knownTypes));

        var format = new System.Text.RegularExpressions.Regex(@"^[A-Z]-\d{2}-\d{2}$");
        Assert.All(allLocations, l => Assert.Matches(format, l.Code));
        Assert.All(allLocations, l => Assert.True(string.IsNullOrEmpty(l.Aisle) == false));
    }

    [Fact]
    public async Task SeedAsync_SeededUsers_HaveValidDemoPasswordHash()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeededUsers_HaveValidDemoPasswordHash));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == "nvhung");
        Assert.NotNull(user);
        Assert.NotNull(user!.PasswordHash);

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash!, "Admin@123");
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public async Task SeedAsync_CreatesFourCategories_AndSixtyProducts()
    {
        await using var db = CreateDb(nameof(SeedAsync_CreatesFourCategories_AndSixtyProducts));
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(4, await db.Categories.CountAsync());
        Assert.Equal(60, summary.Products);
        Assert.Equal(60, await db.Products.CountAsync());

        // Mọi sản phẩm phải có Category hợp lệ
        var categoryIds = await db.Categories.Select(c => c.Id).ToListAsync();
        var productCategoryIds = await db.Products.Select(p => p.CategoryId).Distinct().ToListAsync();
        Assert.All(productCategoryIds, id => Assert.Contains(id, categoryIds));
    }

    [Fact]
    public async Task SeedAsync_Products_HaveUniqueSku_PositivePrice_AndImageUrl()
    {
        await using var db = CreateDb(nameof(SeedAsync_Products_HaveUniqueSku_PositivePrice_AndImageUrl));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var products = await db.Products.ToListAsync();
        Assert.Equal(products.Count, products.Select(p => p.Sku).Distinct().Count());
        Assert.All(products, p => Assert.True(p.Price > 0));
        Assert.All(products, p => Assert.False(string.IsNullOrWhiteSpace(p.Sku)));
        Assert.All(products, p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));
        Assert.All(products, p => Assert.False(string.IsNullOrWhiteSpace(p.ImageUrl)));
    }

    [Fact]
    public async Task SeedAsync_Products_ImageUrl_IsDeterministicBySku()
    {
        await using var db = CreateDb(nameof(SeedAsync_Products_ImageUrl_IsDeterministicBySku));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var iphone = await db.Products.FirstAsync(p => p.Sku == "IP15PM-256-TT");
        Assert.Contains("IP15PM-256-TT", iphone.ImageUrl);
        Assert.StartsWith("https://picsum.photos/seed/", iphone.ImageUrl);

        // Deterministic: dựng lại URL cho cùng SKU bằng đúng công thức → cùng giá trị
        var expected = $"https://picsum.photos/seed/{iphone.Sku}/240/240";
        Assert.Equal(expected, iphone.ImageUrl);
    }

    [Fact]
    public async Task SeedAsync_CreatesVendorsAndCustomers_WithExpectedCounts()
    {
        await using var db = CreateDb(nameof(SeedAsync_CreatesVendorsAndCustomers_WithExpectedCounts));
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.InRange(summary.Vendors, 8, 12);
        Assert.InRange(summary.Customers, 15, 20);
        Assert.Equal(summary.Vendors, await db.Vendors.CountAsync());
        Assert.Equal(summary.Customers, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_VendorsAndCustomers_HaveUniqueNames_AndContactInfo()
    {
        await using var db = CreateDb(nameof(SeedAsync_VendorsAndCustomers_HaveUniqueNames_AndContactInfo));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var vendors = await db.Vendors.ToListAsync();
        var customers = await db.Customers.ToListAsync();

        Assert.Equal(vendors.Count, vendors.Select(v => v.Name).Distinct().Count());
        Assert.Equal(customers.Count, customers.Select(c => c.Name).Distinct().Count());

        Assert.All(vendors, v =>
        {
            Assert.False(string.IsNullOrWhiteSpace(v.Name));
            Assert.False(string.IsNullOrWhiteSpace(v.ContactName));
            Assert.False(string.IsNullOrWhiteSpace(v.Phone));
            Assert.False(string.IsNullOrWhiteSpace(v.Email));
            Assert.False(string.IsNullOrWhiteSpace(v.Address));
        });

        Assert.All(customers, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Name));
            Assert.False(string.IsNullOrWhiteSpace(c.ContactName));
            Assert.False(string.IsNullOrWhiteSpace(c.Phone));
        });
    }

    [Fact]
    public async Task SeedAsync_SeedsStockLedger_Within300To400Movements()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedsStockLedger_Within300To400Movements));
        var seeder = CreateSeeder(db);

        var summary = await seeder.SeedAsync(CancellationToken.None);

        Assert.InRange(summary.StockMovements, 300, 400);
        Assert.Equal(summary.StockMovements, await db.StockMovements.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_StockOnhand_EqualsLedgerSum_AndNeverNegative()
    {
        await using var db = CreateDb(nameof(SeedAsync_StockOnhand_EqualsLedgerSum_AndNeverNegative));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var stocks = await db.Stocks.ToListAsync();
        Assert.NotEmpty(stocks);

        foreach (var stock in stocks)
        {
            var ledger = await db.StockMovements
                .Where(m => m.ProductId == stock.ProductId && m.LocationId == stock.LocationId)
                .ToListAsync();

            int delta = 0;
            foreach (var m in ledger)
            {
                delta += m.MovementType switch
                {
                    MovementType.In => m.Qty,
                    MovementType.Out => -m.Qty,
                    MovementType.Adjustment => m.Qty, // stock counts absolute; handled as delta
                    _ => 0
                };
            }

            Assert.Equal(delta, stock.OnhandQty);
            Assert.True(stock.OnhandQty >= 0, $"Negative stock for product {stock.ProductId}");
        }
    }

    [Fact]
    public async Task SeedAsync_StockMovement_CreatedDates_AreBackdatedWithinWindow()
    {
        await using var db = CreateDb(nameof(SeedAsync_StockMovement_CreatedDates_AreBackdatedWithinWindow));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var now = DateTime.UtcNow;
        // AsNoTracking: ExecuteUpdateAsync ghi thẳng DB, không cập nhật entity đang track
        // → phải đọc lại từ DB (tránh identity resolution trả giá trị cũ).
        var movements = await db.StockMovements.AsNoTracking().ToListAsync();
        Assert.NotEmpty(movements);

        Assert.All(movements, m =>
        {
            var span = now - m.CreatedDate;
            Assert.True(span >= TimeSpan.FromDays(3), $"Movement too recent: {m.CreatedDate}");
            Assert.True(span <= TimeSpan.FromDays(45), $"Movement too old: {m.CreatedDate}");
        });
    }

    [Fact]
    public async Task SeedAsync_LocationCurrentQuantity_RespectsCapacity()
    {
        await using var db = CreateDb(nameof(SeedAsync_LocationCurrentQuantity_RespectsCapacity));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var locations = await db.Locations.ToListAsync();
        var stocks = await db.Stocks.ToListAsync();

        foreach (var location in locations)
        {
            var locationQty = stocks.Where(s => s.LocationId == location.Id).Sum(s => s.OnhandQty);
            Assert.Equal(location.CurrentQuantity, locationQty);
            Assert.True(location.CurrentQuantity <= location.MaxQuantity,
                $"Location {location.Code} over capacity: {location.CurrentQuantity} > {location.MaxQuantity}");
        }
    }

    [Fact]
    public async Task SeedAsync_SeedRun_BackdatesAuditLog_WithHistoricalTimestamps()
    {
        await using var db = CreateDb(nameof(SeedAsync_SeedRun_BackdatesAuditLog_WithHistoricalTimestamps));
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync(CancellationToken.None);

        var auditLogs = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "StockMovement").ToListAsync();
        Assert.NotEmpty(auditLogs);
        Assert.All(auditLogs, a =>
        {
            var span = DateTime.UtcNow - a.OccurredAtUtc;
            Assert.True(span >= TimeSpan.FromDays(3));
            Assert.True(span <= TimeSpan.FromDays(45));
        });
    }

    private static async Task<int> CountUsersInRoleAsync(WmsDbContext db, string roleName)
    {
        var roleId = await db.Roles.Where(r => r.Name == roleName).Select(r => r.Id).FirstOrDefaultAsync();
        if (roleId == Guid.Empty) return 0;
        return await db.UserRoles.CountAsync(ur => ur.RoleId == roleId);
    }
}