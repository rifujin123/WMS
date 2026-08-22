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

    private static readonly string[] DemoCategories =
    {
        "Điện thoại", "Laptop", "Phụ kiện", "Máy tính bảng"
    };

    // (category, sku, name, price VND, unit, dimension) — 60 sản phẩm thị trường VN.
    private static readonly (string Category, string Sku, string Name, decimal Price, string Unit, string Dimension)[] DemoProducts =
    {
        // Điện thoại (18)
        ("Điện thoại", "IP15PM-256-TT",  "iPhone 15 Pro Max 256GB Titan Tự Nhiên", 31_990_000, "Chiếc", "15.9x7.7x0.8 cm"),
        ("Điện thoại", "IP15PM-512-NT",  "iPhone 15 Pro Max 512GB Titan Xanh",     37_990_000, "Chiếc", "15.9x7.7x0.8 cm"),
        ("Điện thoại", "IP15P-128-TD",   "iPhone 15 Pro 128GB Titan Đen",          27_990_000, "Chiếc", "14.6x7.1x0.8 cm"),
        ("Điện thoại", "IP14-128-XD",    "iPhone 14 128GB Xanh Dương",             20_490_000, "Chiếc", "14.7x7.2x0.8 cm"),
        ("Điện thoại", "IP13-128-H",     "iPhone 13 128GB Hồng",                   15_990_000, "Chiếc", "14.7x7.2x0.8 cm"),
        ("Điện thoại", "SS-S24U-256-D",  "Samsung Galaxy S24 Ultra 256GB Đen",     28_990_000, "Chiếc", "16.2x7.9x0.9 cm"),
        ("Điện thoại", "SS-S24-256-T",   "Samsung Galaxy S24 256GB Titan",          21_990_000, "Chiếc", "14.7x7.1x0.8 cm"),
        ("Điện thoại", "SS-S23FE-256-X", "Samsung Galaxy S23 FE 256GB Xanh",       12_990_000, "Chiếc", "15.8x7.6x0.8 cm"),
        ("Điện thoại", "SS-A55-128-X",   "Samsung Galaxy A55 5G 128GB Xanh",        8_490_000, "Chiếc", "16.1x7.7x0.8 cm"),
        ("Điện thoại", "XM-14-256-D",    "Xiaomi 14 256GB Đen",                   18_990_000, "Chiếc", "15.3x7.2x0.8 cm"),
        ("Điện thoại", "XM-RN13-128-X",  "Xiaomi Redmi Note 13 128GB Xanh",        4_990_000, "Chiếc", "16.2x7.5x0.8 cm"),
        ("Điện thoại", "XM-RA3-64-D",    "Xiaomi Redmi A3 64GB Đen",              2_490_000, "Chiếc", "16.3x7.6x0.9 cm"),
        ("Điện thoại", "OP-R11-256-X",   "OPPO Reno 11 256GB Xanh",               10_990_000, "Chiếc", "16.2x7.4x0.8 cm"),
        ("Điện thoại", "OP-A58-128-D",   "OPPO A58 128GB Đen",                     4_690_000, "Chiếc", "16.6x7.6x0.8 cm"),
        ("Điện thoại", "VV-V30-256-X",   "Vivo V30 256GB Xanh",                   12_990_000, "Chiếc", "16.4x7.5x0.8 cm"),
        ("Điện thoại", "GP-P8-128-O",    "Google Pixel 8 128GB Oải hương",        19_990_000, "Chiếc", "15.1x7.1x0.9 cm"),
        ("Điện thoại", "RM-12P-256-X",   "Realme 12 Pro 256GB Xanh",              9_990_000, "Chiếc", "16.2x7.4x0.8 cm"),
        ("Điện thoại", "NK-G42-128-D",   "Nokia G42 128GB Đen",                    3_290_000, "Chiếc", "16.5x7.6x0.9 cm"),
        // Laptop (16)
        ("Laptop", "MBP14-M3-512-X",     "MacBook Pro 14 M3 512GB Xám",           38_990_000, "Chiếc", "31.3x22.1x1.6 cm"),
        ("Laptop", "MBA13-M2-256-X",     "MacBook Air 13 M2 256GB Xám",           22_990_000, "Chiếc", "30.4x21.5x1.1 cm"),
        ("Laptop", "MBP16-M3P-1T-NT",    "MacBook Pro 16 M3 Pro 1TB Đen",         62_990_000, "Chiếc", "35.6x24.8x1.7 cm"),
        ("Laptop", "DELL-XPS13-16-512-D", "Dell XPS 13 i7 16GB/512GB Đen",        30_990_000, "Chiếc", "29.5x19.9x1.5 cm"),
        ("Laptop", "DELL-IN15-8-512-D",  "Dell Inspiron 15 i5 8GB/512GB Đen",     15_990_000, "Chiếc", "35.9x23.5x1.8 cm"),
        ("Laptop", "LEN-TP14-16-512-D",  "Lenovo ThinkPad E14 i5 16GB/512GB Đen", 18_990_000, "Chiếc", "32.5x23.6x1.8 cm"),
        ("Laptop", "LEN-IP5-16-512-X",   "Lenovo IdeaPad Slim 5 i5 16GB/512GB Xám",16_990_000, "Chiếc", "35.6x23.7x1.7 cm"),
        ("Laptop", "HP-PV15-8-512-D",    "HP Pavilion 15 i5 8GB/512GB Đen",       15_490_000, "Chiếc", "35.9x23.6x1.8 cm"),
        ("Laptop", "HP-EB840-16-512-D",  "HP EliteBook 840 G10 i7 16GB/512GB Đen",28_990_000, "Chiếc", "31.6x22.4x1.9 cm"),
        ("Laptop", "ASUS-VB15-8-256-D",  "ASUS VivoBook 15 i3 8GB/256GB Đen",     10_990_000, "Chiếc", "35.9x23.6x1.9 cm"),
        ("Laptop", "ASUS-G16-16-512-D",  "ASUS ROG Strix G16 i7 16GB/512GB Đen",  38_990_000, "Chiếc", "35.4x26.4x2.3 cm"),
        ("Laptop", "ACER-AS5-8-512-D",   "Acer Aspire 5 i5 8GB/512GB Đen",        13_990_000, "Chiếc", "36.3x23.8x1.8 cm"),
        ("Laptop", "ACER-N5-16-512-D",   "Acer Nitro 5 i5 16GB/512GB Đen",        20_990_000, "Chiếc", "36.0x27.1x2.5 cm"),
        ("Laptop", "MS-SL5-8-256-D",     "Microsoft Surface Laptop 5 i5 8GB/256GB Đen", 26_990_000, "Chiếc", "32.3x22.3x1.5 cm"),
        ("Laptop", "SS-GB3P-16-512-X",   "Samsung Galaxy Book3 Pro 16GB/512GB Xám", 29_990_000, "Chiếc", "35.6x25.3x1.2 cm"),
        ("Laptop", "XIAO-RB16-16-512-X", "Xiaomi RedmiBook 16 i5 16GB/512GB Xám", 14_990_000, "Chiếc", "36.1x23.5x1.6 cm"),
        // Phụ kiện (14)
        ("Phụ kiện", "AP-AP2-W",         "AirPods Pro 2",                           5_490_000, "Bộ",   "2.4x2.2x3.1 cm"),
        ("Phụ kiện", "AP-AP3-W",         "AirPods 3",                              3_990_000, "Bộ",   "3.0x1.9x1.9 cm"),
        ("Phụ kiện", "SS-BUDS2-W",       "Samsung Galaxy Buds2 Pro",               3_490_000, "Bộ",   "2.2x2.2x1.9 cm"),
        ("Phụ kiện", "SS-BUDSFE-W",      "Samsung Galaxy Buds FE",                 1_690_000, "Bộ",   "2.0x1.8x2.4 cm"),
        ("Phụ kiện", "XM-BUD4-W",        "Xiaomi Buds 4 Pro",                      1_990_000, "Bộ",   "2.5x2.0x2.0 cm"),
        ("Phụ kiện", "AP-WSE44-M",       "Apple Watch SE 44mm",                    7_490_000, "Chiếc", "4.4x3.8x1.0 cm"),
        ("Phụ kiện", "AP-WS9-M",         "Apple Watch Series 9 45mm",             11_990_000, "Chiếc", "4.5x3.8x1.0 cm"),
        ("Phụ kiện", "ANK-65G-D",        "Sạc GaN Anker 65W",                       890_000, "Chiếc", "7.2x3.6x3.0 cm"),
        ("Phụ kiện", "ANK-PB20-D",       "Sạc dự phòng Anker 20000mAh",            1_290_000, "Chiếc", "15.2x7.1x2.6 cm"),
        ("Phụ kiện", "LOG-MX3S-G",       "Chuột Logitech MX Master 3S",            2_490_000, "Chiếc", "5.1x12.4x4.3 cm"),
        ("Phụ kiện", "LOG-K380-W",       "Bàn phím Logitech K380",                  890_000, "Chiếc", "27.9x12.4x1.6 cm"),
        ("Phụ kiện", "LOG-G502-D",       "Chuột gaming Logitech G502 Hero",        1_190_000, "Chiếc", "7.5x13.2x4.0 cm"),
        ("Phụ kiện", "AP-MK-W",          "Apple Magic Keyboard",                   2_990_000, "Chiếc", "27.9x11.5x0.4 cm"),
        ("Phụ kiện", "SS-T7-1T-D",       "SSD Samsung T7 1TB",                     2_990_000, "Chiếc", "8.5x5.7x0.8 cm"),
        // Máy tính bảng (12)
        ("Máy tính bảng", "IPD-PRO11-M4-S", "iPad Pro 11 M4 256GB Xám",          28_990_000, "Chiếc", "24.9x17.8x0.6 cm"),
        ("Máy tính bảng", "IPD-AIR13-M2-S", "iPad Air 13 M2 256GB Xám",          21_990_000, "Chiếc", "28.1x21.5x0.6 cm"),
        ("Máy tính bảng", "IPD-10-64-S",    "iPad 10 64GB Xanh",                 10_990_000, "Chiếc", "24.9x17.5x0.7 cm"),
        ("Máy tính bảng", "IPD-MINI6-P",    "iPad mini 6 64GB Hồng",              12_990_000, "Chiếc", "19.5x13.5x0.6 cm"),
        ("Máy tính bảng", "SS-TABS9-X",     "Samsung Galaxy Tab S9 256GB Xanh",   19_990_000, "Chiếc", "25.4x16.6x0.6 cm"),
        ("Máy tính bảng", "SS-TABA9-X",     "Samsung Galaxy Tab A9+ 64GB Xanh",    5_990_000, "Chiếc", "25.7x16.8x0.7 cm"),
        ("Máy tính bảng", "XM-PAD6-D",      "Xiaomi Pad 6 128GB Đen",             8_990_000, "Chiếc", "25.4x16.5x0.6 cm"),
        ("Máy tính bảng", "LEN-TABP12-X",   "Lenovo Tab P12 128GB Xám",           7_990_000, "Chiếc", "29.4x18.1x0.7 cm"),
        ("Máy tính bảng", "OP-PAD-D",       "OPPO Pad Air 128GB Xám",              6_990_000, "Chiếc", "25.0x16.4x0.7 cm"),
        ("Máy tính bảng", "VIVO-PAD2-X",    "Vivo Pad2 128GB Xanh",               9_490_000, "Chiếc", "26.7x17.4x0.7 cm"),
        ("Máy tính bảng", "HUAWEI-M11-G",   "Huawei MatePad 11 128GB Xám",       8_490_000, "Chiếc", "25.4x16.2x0.7 cm"),
        ("Máy tính bảng", "ACER-TAB10-D",   "Acer Iconia Tab 10 64GB Đen",        3_990_000, "Chiếc", "24.7x15.6x0.8 cm"),
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
        var (categories, products) = await SeedCategoriesAndProductsAsync(cancellationToken);
        var summary = new SeedSummary
        {
            Users = await SeedUsersAsync(cancellationToken),
            Warehouses = warehouses,
            Locations = locations,
            Categories = categories,
            Products = products
        };

        _logger.LogInformation(
            "Demo data seeding completed (users={Users}, warehouses={Warehouses}, locations={Locations}, categories={Categories}, products={Products}).",
            summary.Users, summary.Warehouses, summary.Locations, summary.Categories, summary.Products);

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

    private async Task<(int Categories, int Products)> SeedCategoriesAndProductsAsync(CancellationToken cancellationToken)
    {
        var categoriesCreated = 0;
        var categoryIds = new Dictionary<string, Guid>();

        foreach (var categoryName in DemoCategories)
        {
            var existing = await _db.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
            if (existing != null)
            {
                categoryIds[categoryName] = existing.Id;
                continue;
            }

            var category = new Category { Id = Guid.NewGuid(), Name = categoryName };
            _db.Categories.Add(category);
            categoryIds[categoryName] = category.Id;
            categoriesCreated++;
        }

        var productsCreated = 0;
        foreach (var (categoryName, sku, name, price, unit, dimension) in DemoProducts)
        {
            if (await _db.Products.AsNoTracking().AnyAsync(p => p.Sku == sku, cancellationToken))
                continue;

            _db.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryIds[categoryName],
                Sku = sku,
                Name = name,
                Unit = unit,
                Price = price,
                Dimension = dimension,
                ImageUrl = PlaceholderImageUrl(sku)
            });
            productsCreated++;
        }

        if (categoriesCreated > 0 || productsCreated > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return (categoriesCreated, productsCreated);
    }

    // Ảnh placeholder stable theo SKU: cùng SKU → cùng ảnh (picsum.photos seed).
    // Chấp nhận phụ thuộc internet khi demo; không dùng Cloudinary trong scope này.
    private static string PlaceholderImageUrl(string sku)
        => $"https://picsum.photos/seed/{sku}/240/240";
}