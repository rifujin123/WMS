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

    // (name, contactName, phone, email, address) — nhà phân phối ủy quyền + đại lý VN.
    private static readonly (string Name, string ContactName, string Phone, string Email, string Address)[] DemoVendors =
    {
        ("Công ty TNHH Thế Giới Di Động",   "Nguyễn Văn Nam",   "028 3838 5000", "contact@thegioididong.vn", "60-62 Lê Lợi, Q.1, TPHCM"),
        ("Công ty Cổ phần FPT Trading",     "Trần Thanh Hải",   "024 3726 5555", "sale@fpttrading.com.vn",    "Số 10A Lý Thường Kiệt, Q.Hoàn Kiếm, Hà Nội"),
        ("Công ty TNHH Samsung Electronics VN", "Lê Thị Hương",  "028 3622 8080", "hcm@samsung.com.vn",      "Số 2 Đường 3/2, Quận 10, TPHCM"),
        ("Công ty TNHH Apple Authorized Reseller VN", "Phạm Minh Châu", "024 3733 8080", "sale@apple-reseller.vn", "25 Đường Láng, Q.Đống Đa, Hà Nội"),
        ("Công ty TNHH Thương mại & Dịch vụ Kỹ thuật Long", "Nguyễn Quốc Long", "028 3999 1234", "info@longtech.com.vn", "88 Nguyễn Trãi, Q.Thanh Xuân, Hà Nội"),
        ("Công ty Cổ phần Sản xuất & TM Phát Đạt", "Hoàng Văn Phát", "024 3356 7890", "phatdat@phatdat.vn", "45 Trần Hưng Đạo, Q.Hoàn Kiếm, Hà Nội"),
        ("Công ty TNHH Anpha Computer",     "Võ An Khoa",       "028 3766 4321", "sales@anphacom.vn",       "120 Nguyễn Chí Thanh, Q.5, TPHCM"),
        ("Công ty TNHH MTV Điện tử Gia Bảo", "Đoàn Thị Mai",     "024 3868 2468", "giabao@giabao.vn",        "68 Quang Trung, Q.Hai Bà Trưng, Hà Nội"),
        ("Công ty TNHH Netco Việt Nam",     "Trịnh Bảo Khang",  "028 3822 1999", "netco@netco.vn",          "33 Nguyễn Văn Cừ, Q.1, TPHCM"),
        ("Công ty Cổ phần E-phone (ENS)",   "Kiều Hạnh Nhi",    "024 3560 8899", "ens@e-phone.vn",          "22 Lê Thanh Nghị, Q.Hai Bà Trưng, Hà Nội"),
    };

    // (name, contactName, phone, email, address) — doanh nghiệp + khách lẻ, sồ/thiếu email/address ở khách lẻ.
    private static readonly (string Name, string ContactName, string Phone, string Email, string Address)[] DemoCustomers =
    {
        ("Công ty CP Đầu tư Minh Anh",      "Bùi Thị Hồng Nhung", "0912 345 678", "minhanh@minhanh.com.vn",   "12 Nguyễn Đình Chiểu, Q.1, TPHCM"),
        ("Công ty TNHH Thương mại Hoàng Gia","Phan Thế Hoàng",    "0903 123 456", "hoanggia@hoanggia.vn",     "56 Lê Văn Sỹ, Q.Tân Bình, TPHCM"),
        ("Công ty Cổ phần Dệt may Việt Thắng", "Lê Minh Đức",    "0988 765 432", "vietthang@vietthang.vn",   "78 Nguyễn Trãi, Q.Thanh Xuân, Hà Nội"),
        ("Công ty TNHH Xây dựng Nam Long",  "Nguyễn Thành Long", "0909 111 222", "namlong@namlong.vn",       "90 Lạc Long Quân, Q.Tây Hồ, Hà Nội"),
        ("Công ty Cổ phần Giáo dục Vina",   "Trần Thu Trang",    "0913 555 666", "vinaedu@vinaedu.vn",       "33 Trần Quốc Vượng, Q.Cầu Giấy, Hà Nội"),
        ("Công ty TNHH Dịch vụ Sắc Màu",    "Hồ Ngọc Ánh",       "0933 777 888", "sacmau@sacmau.vn",         "18 Phạm Văn Đồng, Q.Gò Vấp, TPHCM"),
        ("Công ty CP Công nghệ Bách Khoa",  "Vũ Huy Cường",      "0916 234 567", "bachkhoa@bk-tech.vn",      "101 Trần Hưng Đạo, Q.5, TPHCM"),
        ("Công ty TNHH Kinh doanh ASA",     "Đỗ Thuý Hà",        "0977 888 999", "asa@asa.vn",               "67 Hàng Bài, Q.Hoàn Kiếm, Hà Nội"),
        ("Công ty Cổ phần Thép Miền Nam",   "Lương Văn Tài",     "0989 123 321", "thp@thepmienam.vn",        "24 Tôn Đức Thắng, Q.1, TPHCM"),
        ("Công ty TNHH Truyền thông SkyMedia", "Ngô Thanh Thuỷ", "0917 456 789", "skymedia@skymedia.vn",     "44 Phạm Ngọc Thạch, Q.Đống Đa, Hà Nội"),
        ("Công ty Cổ phần Sữa Bình Minh",   "Đinh Thị Hạnh",     "0902 333 444", "binhminh@bms.vn",          "9 Giảng Võ, Q.Ba Đình, Hà Nội"),
        ("Công ty TNHH Dược phẩm Phú Gia",  "Trương Văn Phúc",   "0935 222 111", "phugia@phugia.vn",         "55 Hoàng Hoa Thám, Q.Ba Đình, Hà Nội"),
        ("Công ty Cổ phần Logistics NorthStar", "Mai Hoàng Dũng", "0944 567 890", "northstar@nslog.vn",       "77 Nguyễn Hữu Thọ, Q.7, TPHCM"),
        ("Khách lẻ - Anh Tuấn",             "Nguyễn Anh Tuấn",   "0908 333 555", "",                        ""),
        ("Khách lẻ - Chị Lan",              "Trần Thị Lan",      "0914 666 777", "",                        ""),
        ("Khách lẻ - Anh Minh",             "Lê Quang Minh",     "0932 444 555", "",                        ""),
        ("Khách lẻ - Chị Hoa",              "Phạm Thu Hoa",      "0905 222 888", "",                        ""),
        ("Khách lẻ - Anh Khoa",             "Đinh Văn Khoa",     "0981 777 123", "",                        ""),
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
        var (vendors, customers) = await SeedVendorsAndCustomersAsync(cancellationToken);
        var stockMovements = await SeedStockAsync(cancellationToken);
        var summary = new SeedSummary
        {
            Users = await SeedUsersAsync(cancellationToken),
            Warehouses = warehouses,
            Locations = locations,
            Categories = categories,
            Products = products,
            Vendors = vendors,
            Customers = customers,
            StockMovements = stockMovements
        };

        _logger.LogInformation(
            "Demo data seeding completed (users={Users}, warehouses={Warehouses}, locations={Locations}, categories={Categories}, products={Products}, vendors={Vendors}, customers={Customers}, stockMovements={StockMovements}).",
            summary.Users, summary.Warehouses, summary.Locations, summary.Categories, summary.Products, summary.Vendors, summary.Customers, summary.StockMovements);

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

    private async Task<(int Vendors, int Customers)> SeedVendorsAndCustomersAsync(CancellationToken cancellationToken)
    {
        var vendorsCreated = 0;
        foreach (var (name, contactName, phone, email, address) in DemoVendors)
        {
            if (await _db.Vendors.AsNoTracking().AnyAsync(v => v.Name == name, cancellationToken))
                continue;
            _db.Vendors.Add(new Vendor
            {
                Id = Guid.NewGuid(),
                Name = name,
                ContactName = contactName,
                Phone = phone,
                Email = email,
                Address = address
            });
            vendorsCreated++;
        }

        var customersCreated = 0;
        foreach (var (name, contactName, phone, email, address) in DemoCustomers)
        {
            if (await _db.Customers.AsNoTracking().AnyAsync(c => c.Name == name, cancellationToken))
                continue;
            _db.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                Name = name,
                ContactName = contactName,
                Phone = phone,
                Email = email,
                Address = address
            });
            customersCreated++;
        }

        if (vendorsCreated > 0 || customersCreated > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return (vendorsCreated, customersCreated);
    }

    private async Task<int> SeedStockAsync(CancellationToken cancellationToken)
    {
        var products = await _db.Products.AsNoTracking().ToListAsync(cancellationToken);
        var storageLocations = await _db.Locations.AsNoTracking()
            .Where(l => l.LocationType == LocationType.Storage)
            .OrderBy(l => l.WarehouseId).ThenBy(l => l.Code)
            .ToListAsync(cancellationToken);
        var actors = await _db.Users.AsNoTracking()
            .Where(u => u.NormalizedUserName == "NVHUNG"
                || u.NormalizedUserName == "NVLAN"
                || u.NormalizedUserName == "PVNAM"
                || u.NormalizedUserName == "NTHAO")
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var actorId = actors.Count > 0 ? actors[0] : Guid.Empty;

        if (products.Count == 0 || storageLocations.Count == 0)
            return 0;

        var stocks = new List<Stock>();
        var movements = new List<StockMovement>();
        var movementTimestamps = new Dictionary<Guid, DateTime>();
        var locationQuantities = new Dictionary<Guid, int>();
        var now = DateTime.UtcNow;

        for (var i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var location = storageLocations[i % storageLocations.Count];

            // Deterministic ledger: In trước rồi Out/Adjustment — không bao giờ tồn âm
            // vì running balance chỉ giảm sau khi đã có đủ In.
            // Ranges được giữ nhỏ để 2 sản phẩm dùng chung 1 storage slot vẫn ≤ MaxQuantity (200).
            var finalOnhand = 10 + ((i * 13) % 40);          // 10..49
            var soldTotal = 15 + ((i * 7) % 30);             // 15..44
            var adjustTotal = 1 + ((i * 5) % 8);             // 1..8
            var totalIn = soldTotal + adjustTotal + finalOnhand;

            var suffix = $"{product.Sku} @ {location.Code}";
            var seq = new (int DayOffset, MovementType Type, int Qty, string Note)[]
            {
                (-42, MovementType.In, totalIn / 2, $"Nhận hàng nhập kho {suffix}"),
                (-35, MovementType.In, totalIn - totalIn / 2, $"Nhận hàng nhập kho {suffix}"),
                (-28, MovementType.Out, soldTotal / 3, $"Bán hàng - xuất kho {suffix}"),
                (-20, MovementType.Adjustment, adjustTotal, $"Kiểm kê - điều chỉnh tồn {suffix}"),
                (-10, MovementType.Out, soldTotal / 3, $"Bán hàng - xuất kho {suffix}"),
                (-3,  MovementType.Out, soldTotal - 2 * (soldTotal / 3), $"Bán hàng - xuất kho {suffix}"),
            };

            var running = 0;
            foreach (var (dayOffset, type, qty, note) in seq)
            {
                running += type switch
                {
                    MovementType.In => qty,
                    MovementType.Out => -qty,
                    MovementType.Adjustment => qty,
                    _ => 0
                };
                if (running < 0) running = 0;

                var occurred = now.AddDays(dayOffset);
                var movement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    LocationId = location.Id,
                    MovementType = type,
                    Qty = qty,
                    Notes = note
                };
                movements.Add(movement);
                movementTimestamps[movement.Id] = occurred;
            }

            // Onhand = running balance thật qua toàn bộ chuỗi ledger
            // (= Σ In − Σ Out + Σ Adjustment), consistency by construction.
            stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                LocationId = location.Id,
                OnhandQty = running,
                ReservedQty = 0
            });
            locationQuantities[location.Id] =
                (locationQuantities.TryGetValue(location.Id, out var existing) ? existing : 0) + running;
        }

        _db.Stocks.AddRange(stocks);
        _db.StockMovements.AddRange(movements);

        var locationsToUpdate = await _db.Locations
            .Where(l => locationQuantities.Keys.Contains(l.Id))
            .ToListAsync(cancellationToken);
        foreach (var loc in locationsToUpdate)
        {
            if (locationQuantities.TryGetValue(loc.Id, out var qty))
                loc.CurrentQuantity = qty;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Backdate CreatedDate + CreatedById (interceptor ép = now khi Add).
        // ExecuteUpdate không qua ChangeTracker → không sinh thêm audit kép.
        foreach (var movement in movements)
        {
            var occurred = movementTimestamps[movement.Id];
            await _db.StockMovements
                .Where(x => x.Id == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreatedDate, occurred)
                    .SetProperty(x => x.CreatedById, actorId == Guid.Empty ? null : (Guid?)actorId),
                    cancellationToken);

            // Backdate luôn AuditLog tự sinh cho movement (interceptor ép OccurredAtUtc = now).
            await _db.AuditLogs
                .Where(a => a.EntityType == nameof(StockMovement) && a.EntityId == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.OccurredAtUtc, occurred)
                    .SetProperty(a => a.ActorUserId, actorId == Guid.Empty ? null : (Guid?)actorId),
                    cancellationToken);
        }

        _logger.LogInformation("Demo stock seeded: {Products} products, {Movements} movements across {Locations} storage locations.",
            products.Count, movements.Count, storageLocations.Count);

        return movements.Count;
    }
}