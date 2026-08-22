using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

/// Seed dữ liệu demo thật (điện thoại / laptop / phụ kiện).
/// Ticket 01: scaffold + gating (Seed:Enabled) + base idempotency + logging.
/// Ticket 02: seed users & roles (2 manager + 4 staff), idempotent theo username.
/// Các nhóm dữ liệu còn lại được các ticket 03–08 điền vào đây.
public class DemoDataSeeder : IDemoDataSeeder
{
    private static readonly string[] DemoRoles = { "Admin", "WarehouseManager", "WarehouseStaff" };

    private readonly WmsDbContext _db;
    private readonly IOptions<DemoSeedOptions> _options;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        WmsDbContext db,
        IOptions<DemoSeedOptions> options,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _options = options;
        _logger = logger;
    }

    public async Task<SeedSummary> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Demo data seeding is disabled (Seed:Enabled=false). Skipping.");
            return SeedSummary.Disabled();
        }

        // Base idempotency: nếu dữ liệu demo đã tồn tại (kho đã có) thì skip toàn bộ
        // để tránh duplicate khi chạy lại / chạy giữa chừng sau một đợt seed trước đó.
        var hasWarehouses = await _db.Warehouses.AsNoTracking().AnyAsync(cancellationToken);
        if (hasWarehouses)
        {
            _logger.LogInformation("Demo data already exists (warehouse present). Skipping to avoid duplicates.");
            return SeedSummary.AlreadySeededSummary();
        }

        var summary = new SeedSummary
        {
            Users = await SeedUsersAsync(cancellationToken)
        };

        _logger.LogInformation(
            "Demo data seeding completed (users={Users}).",
            summary.Users);

        return summary;
    }

    private async Task<int> SeedUsersAsync(CancellationToken cancellationToken)
    {
        // Đảm bảo roles tồn tại trước khi gán user (không tạo kép).
        foreach (var roleName in DemoRoles)
        {
            if (!await _db.Roles.AsNoTracking().AnyAsync(r => r.Name == roleName, cancellationToken))
            {
                _db.Roles.Add(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        var hasher = new PasswordHasher<User>();
        var password = _options.Value.DemoPassword;
        var demoUsers = new[]
        {
            // Managers
            ("manager1", "Nguyễn Văn An",     "manager1@wms.local",     "WarehouseManager"),
            ("manager2", "Trần Thị Bình",     "manager2@wms.local",     "WarehouseManager"),
            // Staff
            ("nvhung",   "Nguyễn Văn Hùng",   "nvhung@wms.local",       "WarehouseStaff"),
            ("nvlan",    "Lê Thị Lan",        "nvlan@wms.local",        "WarehouseStaff"),
            ("pvnam",    "Phạm Văn Nam",      "pvnam@wms.local",        "WarehouseStaff"),
            ("nthao",    "Nguyễn Thị Thảo",   "nthao@wms.local",        "WarehouseStaff"),
        };

        var created = 0;
        foreach (var (username, fullName, email, role) in demoUsers)
        {
            var normalized = username.ToUpperInvariant();
            var exists = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.NormalizedUserName == normalized, cancellationToken);
            if (exists) continue;

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = username,
                NormalizedUserName = normalized,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = hasher.HashPassword(new User { UserName = username }, password)
            };
            var roleId = await _db.Roles
                .Where(r => r.Name == role)
                .Select(r => r.Id)
                .FirstAsync(cancellationToken);

            _db.Users.Add(user);
            _db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roleId });
            created++;
        }

        if (created > 0)
            await _db.SaveChangesAsync(cancellationToken);

        if (created > 0)
            _logger.LogInformation("Demo users seeded: {Created} user(s) (role=WarehouseManager x2, WarehouseStaff x4).", created);

        return created;
    }
}