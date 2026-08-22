using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

/// Seed dữ liệu demo thật (điện thoại / laptop / phụ kiện).
/// Ticket 01: scaffold + gating (Seed:Enabled) + base idempotency + logging.
/// Ticket 02: seed users & roles (2 manager + 4 staff), idempotent theo username.
/// Các nhóm dữ liệu còn lại được các ticket 03–08 điền vào đây.
public class DemoDataSeeder : IDemoDataSeeder
{
    private static readonly string[] DemoRoles = { "Admin", "WarehouseManager", "WarehouseStaff" };

    // (code, name, address, aisles, racks, levels) — HCM 4×3×2=24, HN 5×4×2=40
    private static readonly (string Code, string Name, string Address, int Aisles, int Racks, int Levels)[] DemoWarehouses =
    {
        ("WH-HCM", "Kho TPHCM", "12 Nguyễn Văn Linh, Phường Tân Phú, Quận 7, TP.HCM", 4, 3, 2),
        ("WH-HN",  "Kho Hà Nội", "Số 8 Lê Quang Đạo, Phường Mỹ Đình, Nam Từ Liêm, Hà Nội", 5, 4, 2)
    };

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

        var (warehouses, locations) = await SeedWarehousesAsync(cancellationToken);
        var summary = new SeedSummary
        {
            Users = await SeedUsersAsync(cancellationToken),
            Warehouses = warehouses,
            Locations = locations
        };

        _logger.LogInformation(
            "Demo data seeding completed (users={Users}, warehouses={Warehouses}, locations={Locations}).",
            summary.Users, summary.Warehouses, summary.Locations);

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

    private async Task<(int Warehouses, int Locations)> SeedWarehousesAsync(CancellationToken cancellationToken)
    {
        var (warehouses, locations) = (0, 0);

        foreach (var (code, name, address, aisles, racks, levels) in DemoWarehouses)
        {
            if (await _db.Warehouses.AsNoTracking().AnyAsync(w => w.Code == code, cancellationToken))
                continue;

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Address = address
            };
            _db.Warehouses.Add(warehouse);
            warehouses++;

            var generated = 0;
            for (var a = 0; a < aisles; a++)
            {
                var aisleLetter = ((char)('A' + a)).ToString();
                for (var r = 0; r < racks; r++)
                {
                    for (var l = 0; l < levels; l++)
                    {
                        var rack = (r + 1).ToString("00");
                        var level = (l + 1).ToString("00");
                        var (locationType, maxQty) = LocationSpec(generated);
                        _db.Locations.Add(new Location
                        {
                            Id = Guid.NewGuid(),
                            WarehouseId = warehouse.Id,
                            Code = $"{aisleLetter}-{rack}-{level}",
                            Aisle = aisleLetter,
                            Rack = rack,
                            Level = level,
                            LocationType = locationType,
                            MaxQuantity = maxQty,
                            CurrentQuantity = 0
                        });
                        generated++;
                        locations++;
                    }
                }
            }
        }

        if (warehouses > 0 || locations > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return (warehouses, locations);
    }

    // Kiểu + dung lượng tối đa theo vị trí (deterministic theo thứ tự sinh).
    // Ưu tiên Storage; vài vị trí đầu làm Receiving / Shipping / Picking cho hợp nghiệp vụ.
    private static (LocationType LocationType, int MaxQuantity) LocationSpec(int index) => index switch
    {
        0 => (LocationType.Receiving, 50),
        1 => (LocationType.Shipping, 50),
        2 => (LocationType.Picking, 30),
        _ => (LocationType.Storage, 200)
    };
}