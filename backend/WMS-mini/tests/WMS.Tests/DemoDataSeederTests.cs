using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.Tests;

/// Tests cho demo-data seeder tại seam contract: chạy qua WmsDbContext in-memory.
/// Ticket 01 — gating (Seed:Enabled) + base idempotency (warehouse đã tồn tại → skip).
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
    public async Task SeedAsync_OnFreshDatabase_Runs_AndCanRunAgainAfterDataExists()
    {
        await using var db = CreateDb(nameof(SeedAsync_OnFreshDatabase_Runs_AndCanRunAgainAfterDataExists));
        var seeder = CreateSeeder(db);

        var first = await seeder.SeedAsync(CancellationToken.None);

        Assert.False(first.Skipped);
        Assert.False(first.AlreadySeeded);

        // Mô phỏng: sau khi các ticket 02-08 seed xong, warehouse đã tồn tại → lần chạy sau phải skip.
        db.Warehouses.Add(new Warehouse { Code = "WH-HN", Name = "Kho HN" });
        await db.SaveChangesAsync();

        var second = await seeder.SeedAsync(CancellationToken.None);

        Assert.True(second.AlreadySeeded);
        Assert.Equal(1, await db.Warehouses.CountAsync());
    }
}