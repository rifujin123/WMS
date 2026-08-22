using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
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