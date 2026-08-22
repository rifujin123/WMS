using Microsoft.AspNetCore.Identity;
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

/// Tests cho demo-data seeder tại seam contract: chạy qua WmsDbContext in-memory.
/// Ticket 01 — gating (Seed:Enabled) + base idempotency (warehouse đã tồn tại → skip).
/// Ticket 02 — seed users & roles (2 manager + 4 staff), idempotent theo username.
public class DemoDataSeederTests
{
    private static WmsDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new WmsDbContext(options);
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
        Assert.All(allLocations, l => Assert.Equal(0, l.CurrentQuantity));
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

    private static async Task<int> CountUsersInRoleAsync(WmsDbContext db, string roleName)
    {
        var roleId = await db.Roles.Where(r => r.Name == roleName).Select(r => r.Id).FirstOrDefaultAsync();
        if (roleId == Guid.Empty) return 0;
        return await db.UserRoles.CountAsync(ur => ur.RoleId == roleId);
    }
}