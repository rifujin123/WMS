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

/// <summary>
/// Bộ Seeder dữ liệu demo đa trạng thái (Multi-Status Demo Seeder).
/// Khởi tạo dữ liệu thực tế bao phủ đầy đủ tất cả các trạng thái nghiệp vụ:
/// - PO: Pending, Approved, Received, Closed
/// - Receiving: Draft, Confirmed
/// - PutAwayTask: Open, Assigned, InProgress, Completed
/// - SaleOrder: New, Allocated, Picking, Packed, Shipped
/// - Picking: Open, Assigned, InProgress, Completed
/// - Shipment: Carrier + TrackingNo cho đơn Shipped
/// - StockAdjustment: Draft, Approved
/// - Thẻ kho StockMovement & Sổ cái Onhand/Reserved/Location nhất quán 100%.
/// </summary>
public class MultiStatusDemoDataSeeder : IDemoDataSeeder
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
    private readonly ILogger<MultiStatusDemoDataSeeder> _logger;

    public MultiStatusDemoDataSeeder(
        WmsDbContext db,
        IOptions<DemoSeedOptions> options,
        ILogger<MultiStatusDemoDataSeeder> logger)
    {
        _db = db;
        _options = options;
        _logger = logger;
    }

    public async Task<SeedSummary> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Multi-status demo data seeding is disabled (Seed:Enabled=false). Skipping.");
            return SeedSummary.Disabled();
        }

        var hasWarehouses = await _db.Warehouses.AsNoTracking().AnyAsync(cancellationToken);
        if (hasWarehouses)
        {
            _logger.LogInformation("Demo data already exists (warehouse present). Skipping to avoid duplicates.");
            return SeedSummary.AlreadySeededSummary();
        }

        var usersSeeded = await SeedUsersAsync(cancellationToken);
        var (warehouses, locations) = await SeedWarehousesAsync(cancellationToken);
        var (categories, products) = await SeedCategoriesAndProductsAsync(cancellationToken);
        var (vendors, customers) = await SeedVendorsAndCustomersAsync(cancellationToken);
        await SeedStockAsync(cancellationToken);
        var purchaseOrders = await SeedPoAndPutAwayChainsAsync(cancellationToken);
        var (saleOrders, stockAdjustments) = await SeedSaleOrderPickingAndAdjustmentAsync(cancellationToken);
        
        var totalStockMovements = await _db.StockMovements.AsNoTracking().CountAsync(cancellationToken);
        var summary = new SeedSummary
        {
            Users = usersSeeded,
            Warehouses = warehouses,
            Locations = locations,
            Categories = categories,
            Products = products,
            Vendors = vendors,
            Customers = customers,
            StockMovements = totalStockMovements,
            PurchaseOrders = purchaseOrders,
            SaleOrders = saleOrders,
            StockAdjustments = stockAdjustments
        };

        _logger.LogInformation(
            "Multi-status demo data seeding completed successfully: Users={Users}, Warehouses={Warehouses}, Locations={Locations}, Categories={Categories}, Products={Products}, Vendors={Vendors}, Customers={Customers}, StockMovements={StockMovements}, PurchaseOrders={PurchaseOrders}, SaleOrders={SaleOrders}, StockAdjustments={StockAdjustments}.",
            summary.Users, summary.Warehouses, summary.Locations, summary.Categories, summary.Products, summary.Vendors, summary.Customers, summary.StockMovements, summary.PurchaseOrders, summary.SaleOrders, summary.StockAdjustments);

        return summary;
    }

    private async Task<int> SeedUsersAsync(CancellationToken cancellationToken)
    {
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

            var finalOnhand = 12 + ((i * 13) % 40);          // 12..51
            var soldTotal = 15 + ((i * 7) % 30);             // 15..44
            var adjustTotal = 1 + ((i * 5) % 8);             // 1..8
            var totalIn = soldTotal + adjustTotal + finalOnhand;

            var suffix = $"{product.Sku} @ {location.Code}";
            var seq = new (int DayOffset, MovementType Type, int Qty, string Note)[]
            {
                (-45, MovementType.In, totalIn / 2, $"Nhận hàng nhập kho {suffix}"),
                (-38, MovementType.In, totalIn - totalIn / 2, $"Nhận hàng nhập kho {suffix}"),
                (-30, MovementType.Out, soldTotal / 3, $"Bán hàng - xuất kho {suffix}"),
                (-22, MovementType.Adjustment, adjustTotal, $"Kiểm kê - điều chỉnh tồn {suffix}"),
                (-12, MovementType.Out, soldTotal / 3, $"Bán hàng - xuất kho {suffix}"),
                (-4,  MovementType.Out, soldTotal - 2 * (soldTotal / 3), $"Bán hàng - xuất kho {suffix}"),
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

        foreach (var movement in movements)
        {
            var occurred = movementTimestamps[movement.Id];
            await _db.StockMovements
                .Where(x => x.Id == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreatedDate, occurred)
                    .SetProperty(x => x.CreatedById, actorId == Guid.Empty ? null : (Guid?)actorId),
                    cancellationToken);

            await _db.AuditLogs
                .Where(a => a.EntityType == nameof(StockMovement) && a.EntityId == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.OccurredAtUtc, occurred)
                    .SetProperty(a => a.ActorUserId, actorId == Guid.Empty ? null : (Guid?)actorId),
                    cancellationToken);
        }

        _logger.LogInformation("Multi-status demo stock seeded: {Products} products, {Movements} movements.",
            products.Count, movements.Count);

        return movements.Count;
    }

    private async Task<int> SeedPoAndPutAwayChainsAsync(CancellationToken cancellationToken)
    {
        var vendors = await _db.Vendors.AsNoTracking().ToListAsync(cancellationToken);
        var products = await _db.Products.AsNoTracking().ToListAsync(cancellationToken);
        var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken);
        var manager1Id = users.FirstOrDefault(u => u.NormalizedUserName == "MANAGER1")?.Id ?? Guid.Empty;
        var manager2Id = users.FirstOrDefault(u => u.NormalizedUserName == "MANAGER2")?.Id ?? Guid.Empty;
        var hungStaffId = users.FirstOrDefault(u => u.NormalizedUserName == "NVHUNG")?.Id ?? Guid.Empty;
        var lanStaffId = users.FirstOrDefault(u => u.NormalizedUserName == "NVLAN")?.Id ?? Guid.Empty;
        var namStaffId = users.FirstOrDefault(u => u.NormalizedUserName == "PVNAM")?.Id ?? Guid.Empty;

        if (vendors.Count < 5 || products.Count < 10)
            return 0;

        var existingStocks = await _db.Stocks.AsNoTracking().ToListAsync(cancellationToken);
        var onhand = existingStocks.ToDictionary(s => (s.ProductId, s.LocationId), s => s.OnhandQty);
        var locationQty = (await _db.Locations.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(l => l.Id, l => l.CurrentQuantity);

        var now = DateTime.UtcNow;
        var putAwayMovements = new List<(StockMovement Movement, DateTime Occurred)>();

        // -------------------------------------------------------------
        // PO 1: Pending (Chờ duyệt) — NCC FPT Trading (chưa duyệt, chưa có receiving)
        // -------------------------------------------------------------
        var po1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{now.AddDays(-1):yyyyMMdd}-001",
            VendorName = vendors[1].Name,
            Status = PurchaseOrderStatus.Pending,
            ApprovedById = null,
            ApprovedDate = null
        };
        _db.PurchaseOrders.Add(po1);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po1.Id,
            null, nameof(PurchaseOrderStatus.Pending), "Created", manager1Id, now.AddDays(-1)));

        // -------------------------------------------------------------
        // PO 2: Approved (Đã duyệt) — NCC Apple Reseller
        // Có 1 Phiếu nhận hàng trạng thái Draft (Đang kiểm đếm hàng thực tế)
        // -------------------------------------------------------------
        var po2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{now.AddDays(-3):yyyyMMdd}-002",
            VendorName = vendors[3].Name,
            Status = PurchaseOrderStatus.Approved,
            ApprovedById = manager1Id,
            ApprovedDate = now.AddDays(-2)
        };
        _db.PurchaseOrders.Add(po2);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po2.Id,
            nameof(PurchaseOrderStatus.Pending), nameof(PurchaseOrderStatus.Approved), "StatusChanged", manager1Id, now.AddDays(-2)));

        var rcDraft = new Receiving
        {
            Id = Guid.NewGuid(),
            ReceivingNo = $"RC-{now.AddHours(-4):yyyyMMdd}-001",
            PurchaseOrderId = po2.Id,
            PurchaseOrder = po2,
            Status = ReceivingStatus.Draft,
            ReceivedById = hungStaffId,
            ReceivedDate = now.AddHours(-4),
            ConfirmedById = null,
            ConfirmedDate = null,
            Notes = $"Đang kiểm đếm kiện hàng thực tế từ {vendors[3].Name}",
            ReceivingDetails = new List<ReceivingDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[0].Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 10,
                    Condition = ProductCondition.Ok
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[1].Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 8,
                    Condition = ProductCondition.Damaged
                }
            }
        };
        foreach (var d in rcDraft.ReceivingDetails) d.Receiving = rcDraft;
        _db.Receivings.Add(rcDraft);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Receiving), rcDraft.Id,
            null, nameof(ReceivingStatus.Draft), "Created", hungStaffId, now.AddHours(-4)));

        // -------------------------------------------------------------
        // PO 3: Received (Đã nhận hàng) — NCC Samsung Electronics
        // Receiving Confirmed, sinh 2 PutAway Tasks: 1 Open (chưa gán) & 1 Assigned (đã giao)
        // -------------------------------------------------------------
        var po3 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{now.AddDays(-7):yyyyMMdd}-003",
            VendorName = vendors[2].Name,
            Status = PurchaseOrderStatus.Received,
            ApprovedById = manager2Id,
            ApprovedDate = now.AddDays(-6)
        };
        _db.PurchaseOrders.Add(po3);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po3.Id,
            nameof(PurchaseOrderStatus.Pending), nameof(PurchaseOrderStatus.Approved), "StatusChanged", manager2Id, now.AddDays(-6)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po3.Id,
            nameof(PurchaseOrderStatus.Approved), nameof(PurchaseOrderStatus.Received), "StatusChanged", hungStaffId, now.AddDays(-2)));

        var rc3 = new Receiving
        {
            Id = Guid.NewGuid(),
            ReceivingNo = $"RC-{now.AddDays(-2):yyyyMMdd}-002",
            PurchaseOrderId = po3.Id,
            PurchaseOrder = po3,
            Status = ReceivingStatus.Confirmed,
            ReceivedById = hungStaffId,
            ReceivedDate = now.AddDays(-2),
            ConfirmedById = manager2Id,
            ConfirmedDate = now.AddDays(-2).AddHours(1),
            Notes = $"Nhận hàng Samsung theo đơn {po3.PoNumber}",
            ReceivingDetails = new List<ReceivingDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[2].Id,
                    ExpectedQuantity = 8,
                    ActualQuantity = 8,
                    Condition = ProductCondition.Ok
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[3].Id,
                    ExpectedQuantity = 12,
                    ActualQuantity = 12,
                    Condition = ProductCondition.Ok
                }
            }
        };
        foreach (var d in rc3.ReceivingDetails) d.Receiving = rc3;
        _db.Receivings.Add(rc3);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Receiving), rc3.Id,
            nameof(ReceivingStatus.Draft), nameof(ReceivingStatus.Confirmed), "StatusChanged", manager2Id, now.AddDays(-2).AddHours(1)));

        // PutAway Task 1: Open (chưa gán nhân viên, ToLocationId có thể null hoặc gợi ý)
        var targetLocOpen = FindPutAwayLocation(products[2].Id, 8, locationQty);
        var taskOpen = new PutAwayTask
        {
            Id = Guid.NewGuid(),
            ReceivingDetailId = rc3.ReceivingDetails.First(d => d.ProductId == products[2].Id).Id,
            ProductId = products[2].Id,
            Quantity = 8,
            FromLocationId = null,
            ToLocationId = targetLocOpen?.Id,
            Status = PutAwayTaskStatus.Open,
            AssignToId = null,
            AssignedById = null,
            AssignedDate = null
        };
        _db.PutAwayTasks.Add(taskOpen);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PutAwayTask), taskOpen.Id,
            null, nameof(PutAwayTaskStatus.Open), "Created", manager2Id, now.AddDays(-2).AddHours(1)));

        // PutAway Task 2: Assigned (đã phân công nvhung)
        var targetLocAssigned = FindPutAwayLocation(products[3].Id, 12, locationQty);
        var taskAssigned = new PutAwayTask
        {
            Id = Guid.NewGuid(),
            ReceivingDetailId = rc3.ReceivingDetails.First(d => d.ProductId == products[3].Id).Id,
            ProductId = products[3].Id,
            Quantity = 12,
            FromLocationId = null,
            ToLocationId = targetLocAssigned?.Id,
            Status = PutAwayTaskStatus.Assigned,
            AssignToId = hungStaffId,
            AssignedById = manager2Id,
            AssignedDate = now.AddHours(-6)
        };
        _db.PutAwayTasks.Add(taskAssigned);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PutAwayTask), taskAssigned.Id,
            nameof(PutAwayTaskStatus.Open), nameof(PutAwayTaskStatus.Assigned), "StatusChanged", manager2Id, now.AddHours(-6)));

        // -------------------------------------------------------------
        // PO 4: Received (Đang cất hàng InProgress) — NCC Thế Giới Di Động
        // -------------------------------------------------------------
        var po4 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{now.AddDays(-12):yyyyMMdd}-004",
            VendorName = vendors[0].Name,
            Status = PurchaseOrderStatus.Received,
            ApprovedById = manager1Id,
            ApprovedDate = now.AddDays(-11)
        };
        _db.PurchaseOrders.Add(po4);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po4.Id,
            nameof(PurchaseOrderStatus.Pending), nameof(PurchaseOrderStatus.Approved), "StatusChanged", manager1Id, now.AddDays(-11)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po4.Id,
            nameof(PurchaseOrderStatus.Approved), nameof(PurchaseOrderStatus.Received), "StatusChanged", lanStaffId, now.AddDays(-4)));

        var rc4 = new Receiving
        {
            Id = Guid.NewGuid(),
            ReceivingNo = $"RC-{now.AddDays(-4):yyyyMMdd}-003",
            PurchaseOrderId = po4.Id,
            PurchaseOrder = po4,
            Status = ReceivingStatus.Confirmed,
            ReceivedById = lanStaffId,
            ReceivedDate = now.AddDays(-4),
            ConfirmedById = manager1Id,
            ConfirmedDate = now.AddDays(-4).AddHours(2),
            Notes = $"Nhận hàng {po4.PoNumber}",
            ReceivingDetails = new List<ReceivingDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[4].Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 10,
                    Condition = ProductCondition.Ok
                }
            }
        };
        foreach (var d in rc4.ReceivingDetails) d.Receiving = rc4;
        _db.Receivings.Add(rc4);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Receiving), rc4.Id,
            nameof(ReceivingStatus.Draft), nameof(ReceivingStatus.Confirmed), "StatusChanged", manager1Id, now.AddDays(-4).AddHours(2)));

        // PutAway Task 3: InProgress (đang cất bởi lanStaff)
        var targetLocInProgress = FindPutAwayLocation(products[4].Id, 10, locationQty);
        var taskInProgress = new PutAwayTask
        {
            Id = Guid.NewGuid(),
            ReceivingDetailId = rc4.ReceivingDetails.First().Id,
            ProductId = products[4].Id,
            Quantity = 10,
            FromLocationId = null,
            ToLocationId = targetLocInProgress?.Id,
            Status = PutAwayTaskStatus.InProgress,
            AssignToId = lanStaffId,
            AssignedById = manager1Id,
            AssignedDate = now.AddDays(-3),
            StartedById = lanStaffId,
            StartedDate = now.AddHours(-1)
        };
        _db.PutAwayTasks.Add(taskInProgress);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PutAwayTask), taskInProgress.Id,
            nameof(PutAwayTaskStatus.Open), nameof(PutAwayTaskStatus.Assigned), "StatusChanged", manager1Id, now.AddDays(-3)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PutAwayTask), taskInProgress.Id,
            nameof(PutAwayTaskStatus.Assigned), nameof(PutAwayTaskStatus.InProgress), "StatusChanged", lanStaffId, now.AddHours(-1)));

        // -------------------------------------------------------------
        // PO 5 & PO 6: Closed (Đã hoàn tất toàn bộ PutAway Completed)
        // -------------------------------------------------------------
        var closedChains = new[]
        {
            (PoNo: $"PO-{now.AddDays(-28):yyyyMMdd}-005", Vendor: vendors[4], ProdIdxs: new[] { 5, 6 }, QtyPer: 8, Day: 28),
            (PoNo: $"PO-{now.AddDays(-20):yyyyMMdd}-006", Vendor: vendors[0], ProdIdxs: new[] { 7, 8 }, QtyPer: 6, Day: 20),
        };

        foreach (var (poNo, vendor, prodIdxs, qtyPer, day) in closedChains)
        {
            var poClosed = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                PoNumber = poNo,
                VendorName = vendor.Name,
                Status = PurchaseOrderStatus.Closed,
                ApprovedById = manager1Id,
                ApprovedDate = now.AddDays(-day),
                ClosedById = manager1Id,
                ClosedDate = now.AddDays(-day + 8)
            };
            _db.PurchaseOrders.Add(poClosed);

            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), poClosed.Id,
                nameof(PurchaseOrderStatus.Pending), nameof(PurchaseOrderStatus.Approved), "StatusChanged", manager1Id, now.AddDays(-day)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), poClosed.Id,
                nameof(PurchaseOrderStatus.Approved), nameof(PurchaseOrderStatus.Received), "StatusChanged", namStaffId, now.AddDays(-day + 2)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), poClosed.Id,
                nameof(PurchaseOrderStatus.Received), nameof(PurchaseOrderStatus.Closed), "StatusChanged", manager1Id, now.AddDays(-day + 8)));

            var rcClosed = new Receiving
            {
                Id = Guid.NewGuid(),
                ReceivingNo = $"RC-{now.AddDays(-day + 2):yyyyMMdd}-{poNo.Substring(poNo.Length - 3)}",
                PurchaseOrderId = poClosed.Id,
                PurchaseOrder = poClosed,
                Status = ReceivingStatus.Confirmed,
                ReceivedById = namStaffId,
                ReceivedDate = now.AddDays(-day + 2),
                ConfirmedById = manager1Id,
                ConfirmedDate = now.AddDays(-day + 3),
                Notes = $"Nhận hàng hoàn tất {poClosed.PoNumber}",
                ReceivingDetails = prodIdxs.Select(i => new ReceivingDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[i].Id,
                    ExpectedQuantity = qtyPer,
                    ActualQuantity = qtyPer,
                    Condition = ProductCondition.Ok
                }).ToList()
            };
            foreach (var d in rcClosed.ReceivingDetails) d.Receiving = rcClosed;
            _db.Receivings.Add(rcClosed);

            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Receiving), rcClosed.Id,
                nameof(ReceivingStatus.Draft), nameof(ReceivingStatus.Confirmed), "StatusChanged", manager1Id, now.AddDays(-day + 3)));

            foreach (var detail in rcClosed.ReceivingDetails)
            {
                var targetLoc = FindPutAwayLocation(detail.ProductId, detail.ActualQuantity, locationQty);
                if (targetLoc is null) continue;

                var before = onhand.GetValueOrDefault((detail.ProductId, targetLoc.Id));
                onhand[(detail.ProductId, targetLoc.Id)] = before + detail.ActualQuantity;
                locationQty[targetLoc.Id] = locationQty.GetValueOrDefault(targetLoc.Id) + detail.ActualQuantity;

                var putAway = new PutAwayTask
                {
                    Id = Guid.NewGuid(),
                    ReceivingDetailId = detail.Id,
                    ReceivingDetail = detail,
                    ProductId = detail.ProductId,
                    Quantity = detail.ActualQuantity,
                    FromLocationId = null,
                    ToLocationId = targetLoc.Id,
                    Status = PutAwayTaskStatus.Completed,
                    AssignToId = namStaffId,
                    AssignedById = manager1Id,
                    AssignedDate = now.AddDays(-day + 4),
                    StartedById = namStaffId,
                    StartedDate = now.AddDays(-day + 5),
                    CompletedById = namStaffId,
                    CompletedDate = now.AddDays(-day + 6),
                };
                _db.PutAwayTasks.Add(putAway);

                _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PutAwayTask), putAway.Id,
                    nameof(PutAwayTaskStatus.Open), nameof(PutAwayTaskStatus.Assigned), "StatusChanged", manager1Id, now.AddDays(-day + 4)));
                _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PutAwayTask), putAway.Id,
                    nameof(PutAwayTaskStatus.Assigned), nameof(PutAwayTaskStatus.InProgress), "StatusChanged", namStaffId, now.AddDays(-day + 5)));
                _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PutAwayTask), putAway.Id,
                    nameof(PutAwayTaskStatus.InProgress), nameof(PutAwayTaskStatus.Completed), "StatusChanged", namStaffId, now.AddDays(-day + 6)));

                var putAwayOccurred = now.AddDays(-day + 6);
                var putAwayMovement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = detail.ProductId,
                    LocationId = targetLoc.Id,
                    MovementType = MovementType.In,
                    Qty = detail.ActualQuantity,
                    Notes = $"Phiếu cất {poClosed.PoNumber} - nhập kho {targetLoc.Code}"
                };
                _db.StockMovements.Add(putAwayMovement);
                putAwayMovements.Add((putAwayMovement, putAwayOccurred));
            }
        }

        // Cập nhật lại Onhand và Location Quantity vào DB
        foreach (var ((productId, locId), qty) in onhand)
        {
            var stock = await _db.Stocks.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locId, cancellationToken);
            if (stock != null)
            {
                var tracked = _db.Stocks.Find(stock.Id);
                if (tracked != null) tracked.OnhandQty = qty;
            }
        }
        foreach (var (locId, qty) in locationQty)
        {
            var loc = await _db.Locations.FindAsync(new object[] { locId }, cancellationToken);
            if (loc != null) loc.CurrentQuantity = qty;
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var (movement, occurred) in putAwayMovements)
        {
            await _db.StockMovements
                .Where(x => x.Id == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreatedDate, occurred)
                    .SetProperty(x => x.CreatedById, namStaffId == Guid.Empty ? null : (Guid?)namStaffId),
                    cancellationToken);
            await _db.AuditLogs
                .Where(a => a.EntityType == nameof(StockMovement) && a.EntityId == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.OccurredAtUtc, occurred)
                    .SetProperty(a => a.ActorUserId, namStaffId == Guid.Empty ? null : (Guid?)namStaffId),
                    cancellationToken);
        }

        var totalPoCount = 5;
        _logger.LogInformation("Multi-status Inbound seeded: POs with all statuses (Pending, Approved, Received, Closed), Receivings (Draft, Confirmed), PutAway (Open, Assigned, InProgress, Completed).");

        return totalPoCount;
    }

    private async Task<(int SaleOrders, int StockAdjustments)> SeedSaleOrderPickingAndAdjustmentAsync(CancellationToken cancellationToken)
    {
        var customers = await _db.Customers.AsNoTracking().ToListAsync(cancellationToken);
        var products = await _db.Products.AsNoTracking().ToListAsync(cancellationToken);
        var warehouses = await _db.Warehouses.AsNoTracking().ToListAsync(cancellationToken);
        var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken);
        var hungStaffId = users.FirstOrDefault(u => u.NormalizedUserName == "NVHUNG")?.Id ?? Guid.Empty;
        var lanStaffId = users.FirstOrDefault(u => u.NormalizedUserName == "NVLAN")?.Id ?? Guid.Empty;
        var namStaffId = users.FirstOrDefault(u => u.NormalizedUserName == "PVNAM")?.Id ?? Guid.Empty;
        var thaoStaffId = users.FirstOrDefault(u => u.NormalizedUserName == "NTHAO")?.Id ?? Guid.Empty;
        var manager1Id = users.FirstOrDefault(u => u.NormalizedUserName == "MANAGER1")?.Id ?? Guid.Empty;

        if (customers.Count < 7 || products.Count < 25 || warehouses.Count == 0)
            return (0, 0);

        var existingStocks = await _db.Stocks.AsNoTracking().ToListAsync(cancellationToken);
        var onhand = existingStocks.ToDictionary(s => (s.ProductId, s.LocationId), s => s.OnhandQty);
        var reserved = existingStocks.ToDictionary(s => (s.ProductId, s.LocationId), s => s.ReservedQty);
        var locationQty = (await _db.Locations.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(l => l.Id, l => l.CurrentQuantity);

        var now = DateTime.UtcNow;
        var outboundMovements = new List<(StockMovement Movement, DateTime Occurred)>();
        var warehouseHcm = warehouses[0];
        var warehouseHn = warehouses.Count > 1 ? warehouses[1] : warehouses[0];

        // -------------------------------------------------------------
        // SO 1: New (Mới) — Khách Minh Anh (chưa phân bổ, chưa lấy hàng)
        // -------------------------------------------------------------
        var soNew = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = $"SO-{now.AddHours(-6):yyyyMMdd}-001",
            CustomerName = customers[0].Name,
            OrderDate = now.AddHours(-6),
            Status = SaleOrderStatus.New,
            SaleOrderDetails = new List<SaleOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[9].Id,
                    Quantity = 2,
                    AllocatedQty = 0,
                    Status = SaleOrderDetailStatus.Pending
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[10].Id,
                    Quantity = 3,
                    AllocatedQty = 0,
                    Status = SaleOrderDetailStatus.Pending
                }
            }
        };
        foreach (var d in soNew.SaleOrderDetails) d.SaleOrder = soNew;
        _db.SaleOrders.Add(soNew);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soNew.Id,
            null, nameof(SaleOrderStatus.New), "Created", hungStaffId, now.AddHours(-6)));

        // -------------------------------------------------------------
        // SO 2: Allocated (Đã phân bổ) — Khách Hoàng Gia
        // Sinh Picking: Open (chưa giao)
        // -------------------------------------------------------------
        var soAllocated = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = $"SO-{now.AddDays(-1):yyyyMMdd}-002",
            CustomerName = customers[1].Name,
            OrderDate = now.AddDays(-1),
            Status = SaleOrderStatus.Allocated,
            SaleOrderDetails = new List<SaleOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[11].Id,
                    Quantity = 2,
                    AllocatedQty = 2,
                    Status = SaleOrderDetailStatus.Allocated
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[12].Id,
                    Quantity = 1,
                    AllocatedQty = 1,
                    Status = SaleOrderDetailStatus.Allocated
                }
            }
        };
        foreach (var d in soAllocated.SaleOrderDetails) d.SaleOrder = soAllocated;
        _db.SaleOrders.Add(soAllocated);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soAllocated.Id,
            nameof(SaleOrderStatus.New), nameof(SaleOrderStatus.Allocated), "StatusChanged", manager1Id, now.AddDays(-1).AddHours(1)));

        // Tăng ReservedQty
        var locProd11 = FindStockLocation(products[11].Id, onhand);
        if (locProd11 != null) reserved[(products[11].Id, locProd11.Id)] = reserved.GetValueOrDefault((products[11].Id, locProd11.Id)) + 2;
        var locProd12 = FindStockLocation(products[12].Id, onhand);
        if (locProd12 != null) reserved[(products[12].Id, locProd12.Id)] = reserved.GetValueOrDefault((products[12].Id, locProd12.Id)) + 1;

        // Picking Open
        var pkOpen = new Picking
        {
            Id = Guid.NewGuid(),
            PickingNo = $"PK-{now.AddHours(-18):yyyyMMdd}-001",
            WarehouseId = warehouseHcm.Id,
            Status = PickingStatus.Open,
            AssignedToId = null,
            AssignedById = null,
            AssignedDate = null,
            PickingDetails = soAllocated.SaleOrderDetails.Select(d => new PickingDetail
            {
                Id = Guid.NewGuid(),
                ProductId = d.ProductId,
                SaleOrderDetailId = d.Id,
                SaleOrderDetail = d,
                QtyToPick = d.Quantity,
                QtyPicked = 0,
                Status = PickingDetailStatus.Pending,
                LocationId = FindStockLocation(d.ProductId, onhand)?.Id
            }).ToList()
        };
        foreach (var pd in pkOpen.PickingDetails) pd.Picking = pkOpen;
        _db.Pickings.Add(pkOpen);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Picking), pkOpen.Id,
            null, nameof(PickingStatus.Open), "Created", manager1Id, now.AddHours(-18)));

        // -------------------------------------------------------------
        // SO 3: Picking (Đã giao việc) — Khách Dệt may Việt Thắng
        // Sinh Picking: Assigned (đã phân công nvhung)
        // -------------------------------------------------------------
        var soPickingAssigned = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = $"SO-{now.AddDays(-2):yyyyMMdd}-003",
            CustomerName = customers[2].Name,
            OrderDate = now.AddDays(-2),
            Status = SaleOrderStatus.Picking,
            SaleOrderDetails = new List<SaleOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[13].Id,
                    Quantity = 3,
                    AllocatedQty = 3,
                    Status = SaleOrderDetailStatus.Allocated
                }
            }
        };
        foreach (var d in soPickingAssigned.SaleOrderDetails) d.SaleOrder = soPickingAssigned;
        _db.SaleOrders.Add(soPickingAssigned);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soPickingAssigned.Id,
            nameof(SaleOrderStatus.New), nameof(SaleOrderStatus.Allocated), "StatusChanged", manager1Id, now.AddDays(-2)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soPickingAssigned.Id,
            nameof(SaleOrderStatus.Allocated), nameof(SaleOrderStatus.Picking), "StatusChanged", manager1Id, now.AddDays(-2).AddHours(2)));

        var locProd13 = FindStockLocation(products[13].Id, onhand);
        if (locProd13 != null) reserved[(products[13].Id, locProd13.Id)] = reserved.GetValueOrDefault((products[13].Id, locProd13.Id)) + 3;

        var pkAssigned = new Picking
        {
            Id = Guid.NewGuid(),
            PickingNo = $"PK-{now.AddDays(-2):yyyyMMdd}-002",
            WarehouseId = warehouseHcm.Id,
            Status = PickingStatus.Assigned,
            AssignedToId = hungStaffId,
            AssignedById = manager1Id,
            AssignedDate = now.AddDays(-2).AddHours(2),
            PickingDetails = soPickingAssigned.SaleOrderDetails.Select(d => new PickingDetail
            {
                Id = Guid.NewGuid(),
                ProductId = d.ProductId,
                SaleOrderDetailId = d.Id,
                SaleOrderDetail = d,
                QtyToPick = d.Quantity,
                QtyPicked = 0,
                Status = PickingDetailStatus.Pending,
                LocationId = FindStockLocation(d.ProductId, onhand)?.Id
            }).ToList()
        };
        foreach (var pd in pkAssigned.PickingDetails) pd.Picking = pkAssigned;
        _db.Pickings.Add(pkAssigned);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Picking), pkAssigned.Id,
            nameof(PickingStatus.Open), nameof(PickingStatus.Assigned), "StatusChanged", manager1Id, now.AddDays(-2).AddHours(2)));

        // -------------------------------------------------------------
        // SO 4: Picking (Đang lấy hàng InProgress) — Khách Nam Long
        // Sinh Picking: InProgress (nthao đang đi gom hàng tại kệ)
        // -------------------------------------------------------------
        var soPickingInProgress = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = $"SO-{now.AddDays(-3):yyyyMMdd}-004",
            CustomerName = customers[3].Name,
            OrderDate = now.AddDays(-3),
            Status = SaleOrderStatus.Picking,
            SaleOrderDetails = new List<SaleOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[14].Id,
                    Quantity = 4,
                    AllocatedQty = 4,
                    Status = SaleOrderDetailStatus.Allocated
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[15].Id,
                    Quantity = 2,
                    AllocatedQty = 2,
                    Status = SaleOrderDetailStatus.Allocated
                }
            }
        };
        foreach (var d in soPickingInProgress.SaleOrderDetails) d.SaleOrder = soPickingInProgress;
        _db.SaleOrders.Add(soPickingInProgress);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soPickingInProgress.Id,
            nameof(SaleOrderStatus.New), nameof(SaleOrderStatus.Allocated), "StatusChanged", manager1Id, now.AddDays(-3)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soPickingInProgress.Id,
            nameof(SaleOrderStatus.Allocated), nameof(SaleOrderStatus.Picking), "StatusChanged", manager1Id, now.AddDays(-3).AddHours(1)));

        var locProd14 = FindStockLocation(products[14].Id, onhand);
        if (locProd14 != null) reserved[(products[14].Id, locProd14.Id)] = reserved.GetValueOrDefault((products[14].Id, locProd14.Id)) + 4;
        var locProd15 = FindStockLocation(products[15].Id, onhand);
        if (locProd15 != null) reserved[(products[15].Id, locProd15.Id)] = reserved.GetValueOrDefault((products[15].Id, locProd15.Id)) + 2;

        var pkInProgress = new Picking
        {
            Id = Guid.NewGuid(),
            PickingNo = $"PK-{now.AddDays(-3):yyyyMMdd}-003",
            WarehouseId = warehouseHcm.Id,
            Status = PickingStatus.InProgress,
            AssignedToId = thaoStaffId,
            AssignedById = manager1Id,
            AssignedDate = now.AddDays(-3).AddHours(1),
            StartedById = thaoStaffId,
            StartedDate = now.AddHours(-2),
            PickingDetails = new List<PickingDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[14].Id,
                    SaleOrderDetailId = soPickingInProgress.SaleOrderDetails.First(d => d.ProductId == products[14].Id).Id,
                    SaleOrderDetail = soPickingInProgress.SaleOrderDetails.First(d => d.ProductId == products[14].Id),
                    QtyToPick = 4,
                    QtyPicked = 4,
                    Status = PickingDetailStatus.Picked,
                    LocationId = FindStockLocation(products[14].Id, onhand)?.Id
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[15].Id,
                    SaleOrderDetailId = soPickingInProgress.SaleOrderDetails.First(d => d.ProductId == products[15].Id).Id,
                    SaleOrderDetail = soPickingInProgress.SaleOrderDetails.First(d => d.ProductId == products[15].Id),
                    QtyToPick = 2,
                    QtyPicked = 1,
                    Status = PickingDetailStatus.Pending,
                    LocationId = FindStockLocation(products[15].Id, onhand)?.Id
                }
            }
        };
        foreach (var pd in pkInProgress.PickingDetails) pd.Picking = pkInProgress;
        _db.Pickings.Add(pkInProgress);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Picking), pkInProgress.Id,
            nameof(PickingStatus.Open), nameof(PickingStatus.Assigned), "StatusChanged", manager1Id, now.AddDays(-3).AddHours(1)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Picking), pkInProgress.Id,
            nameof(PickingStatus.Assigned), nameof(PickingStatus.InProgress), "StatusChanged", thaoStaffId, now.AddHours(-2)));

        // -------------------------------------------------------------
        // SO 5: Packed (Đã đóng gói) — Khách Giáo dục Vina
        // Picking Completed -> trừ tồn Onhand + sinh Out movement
        // -------------------------------------------------------------
        var soPacked = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = $"SO-{now.AddDays(-5):yyyyMMdd}-005",
            CustomerName = customers[4].Name,
            OrderDate = now.AddDays(-5),
            Status = SaleOrderStatus.Packed,
            PackedById = lanStaffId,
            PackedDate = now.AddHours(-4),
            SaleOrderDetails = new List<SaleOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[16].Id,
                    Quantity = 3,
                    AllocatedQty = 3,
                    Status = SaleOrderDetailStatus.Picked
                }
            }
        };
        foreach (var d in soPacked.SaleOrderDetails) d.SaleOrder = soPacked;
        _db.SaleOrders.Add(soPacked);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soPacked.Id,
            nameof(SaleOrderStatus.New), nameof(SaleOrderStatus.Allocated), "StatusChanged", manager1Id, now.AddDays(-5)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soPacked.Id,
            nameof(SaleOrderStatus.Allocated), nameof(SaleOrderStatus.Picking), "StatusChanged", manager1Id, now.AddDays(-4)));
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soPacked.Id,
            nameof(SaleOrderStatus.Picking), nameof(SaleOrderStatus.Packed), "StatusChanged", lanStaffId, now.AddHours(-4)));

        var pkPacked = new Picking
        {
            Id = Guid.NewGuid(),
            PickingNo = $"PK-{now.AddDays(-5):yyyyMMdd}-004",
            WarehouseId = warehouseHcm.Id,
            Status = PickingStatus.Completed,
            AssignedToId = lanStaffId,
            AssignedById = manager1Id,
            AssignedDate = now.AddDays(-4),
            StartedById = lanStaffId,
            StartedDate = now.AddDays(-4).AddHours(1),
            CompletedById = lanStaffId,
            CompletedDate = now.AddHours(-5),
            PickingDetails = soPacked.SaleOrderDetails.Select(d => new PickingDetail
            {
                Id = Guid.NewGuid(),
                ProductId = d.ProductId,
                SaleOrderDetailId = d.Id,
                SaleOrderDetail = d,
                QtyToPick = d.Quantity,
                QtyPicked = d.Quantity,
                Status = PickingDetailStatus.Picked,
                LocationId = FindStockLocation(d.ProductId, onhand)?.Id
            }).ToList()
        };
        foreach (var pd in pkPacked.PickingDetails) pd.Picking = pkPacked;
        _db.Pickings.Add(pkPacked);
        _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Picking), pkPacked.Id,
            nameof(PickingStatus.Open), nameof(PickingStatus.Completed), "StatusChanged", lanStaffId, now.AddHours(-5)));

        foreach (var pd in pkPacked.PickingDetails)
        {
            if (pd.LocationId is not Guid locId) continue;
            var key = (pd.ProductId, locId);
            var before = onhand.GetValueOrDefault(key);
            onhand[key] = Math.Max(0, before - pd.QtyPicked);
            locationQty[locId] = Math.Max(0, locationQty.GetValueOrDefault(locId) - pd.QtyPicked);

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = pd.ProductId,
                LocationId = locId,
                MovementType = MovementType.Out,
                Qty = pd.QtyPicked,
                Notes = $"Phiếu lấy hàng {pkPacked.PickingNo} - xuất kho đóng gói"
            };
            _db.StockMovements.Add(movement);
            outboundMovements.Add((movement, now.AddHours(-5)));
        }

        // -------------------------------------------------------------
        // SO 6 & SO 7: Shipped (Đã giao hàng) — Có Vận đơn Shipment
        // -------------------------------------------------------------
        var shippedSpecs = new[]
        {
            (OrderNo: $"SO-{now.AddDays(-8):yyyyMMdd}-006", Cust: customers[5], ProdIdx: 17, Qty: 4, Carrier: "Giao Hàng Tiết Kiệm (GHTK)", Tracking: "GHTK-HCM-98217348", Day: 8),
            (OrderNo: $"SO-{now.AddDays(-14):yyyyMMdd}-007", Cust: customers[6], ProdIdx: 18, Qty: 2, Carrier: "Viettel Post", Tracking: "VTP-HN-44120938", Day: 14)
        };

        foreach (var (orderNo, cust, prodIdx, qty, carrier, tracking, day) in shippedSpecs)
        {
            var soShipped = new SaleOrder
            {
                Id = Guid.NewGuid(),
                OrderNo = orderNo,
                CustomerName = cust.Name,
                OrderDate = now.AddDays(-day),
                Status = SaleOrderStatus.Shipped,
                PackedById = namStaffId,
                PackedDate = now.AddDays(-day + 2),
                SaleOrderDetails = new List<SaleOrderDetail>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = products[prodIdx].Id,
                        Quantity = qty,
                        AllocatedQty = qty,
                        Status = SaleOrderDetailStatus.Picked
                    }
                }
            };
            foreach (var d in soShipped.SaleOrderDetails) d.SaleOrder = soShipped;
            _db.SaleOrders.Add(soShipped);

            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soShipped.Id,
                nameof(SaleOrderStatus.New), nameof(SaleOrderStatus.Allocated), "StatusChanged", manager1Id, now.AddDays(-day)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soShipped.Id,
                nameof(SaleOrderStatus.Allocated), nameof(SaleOrderStatus.Picking), "StatusChanged", manager1Id, now.AddDays(-day + 1)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soShipped.Id,
                nameof(SaleOrderStatus.Picking), nameof(SaleOrderStatus.Packed), "StatusChanged", namStaffId, now.AddDays(-day + 2)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), soShipped.Id,
                nameof(SaleOrderStatus.Packed), nameof(SaleOrderStatus.Shipped), "StatusChanged", manager1Id, now.AddDays(-day + 3)));

            var pkShipped = new Picking
            {
                Id = Guid.NewGuid(),
                PickingNo = $"PK-{now.AddDays(-day):yyyyMMdd}-{orderNo.Substring(orderNo.Length - 3)}",
                WarehouseId = warehouseHn.Id,
                Status = PickingStatus.Completed,
                AssignedToId = namStaffId,
                AssignedById = manager1Id,
                AssignedDate = now.AddDays(-day + 1),
                StartedById = namStaffId,
                StartedDate = now.AddDays(-day + 1).AddHours(1),
                CompletedById = namStaffId,
                CompletedDate = now.AddDays(-day + 2),
                PickingDetails = soShipped.SaleOrderDetails.Select(d => new PickingDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = d.ProductId,
                    SaleOrderDetailId = d.Id,
                    SaleOrderDetail = d,
                    QtyToPick = d.Quantity,
                    QtyPicked = d.Quantity,
                    Status = PickingDetailStatus.Picked,
                    LocationId = FindStockLocation(d.ProductId, onhand)?.Id
                }).ToList()
            };
            foreach (var pd in pkShipped.PickingDetails) pd.Picking = pkShipped;
            _db.Pickings.Add(pkShipped);

            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Picking), pkShipped.Id,
                nameof(PickingStatus.Open), nameof(PickingStatus.Completed), "StatusChanged", namStaffId, now.AddDays(-day + 2)));

            foreach (var pd in pkShipped.PickingDetails)
            {
                if (pd.LocationId is not Guid locId) continue;
                var key = (pd.ProductId, locId);
                var before = onhand.GetValueOrDefault(key);
                onhand[key] = Math.Max(0, before - pd.QtyPicked);
                locationQty[locId] = Math.Max(0, locationQty.GetValueOrDefault(locId) - pd.QtyPicked);

                var movement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = pd.ProductId,
                    LocationId = locId,
                    MovementType = MovementType.Out,
                    Qty = pd.QtyPicked,
                    Notes = $"Phiếu lấy hàng {pkShipped.PickingNo} - xuất kho giao vận"
                };
                _db.StockMovements.Add(movement);
                outboundMovements.Add((movement, now.AddDays(-day + 2)));
            }

            // Ghi nhận Shipment
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                SaleOrderId = soShipped.Id,
                SaleOrder = soShipped,
                Carrier = carrier,
                TrackingNo = tracking,
                ShippedDate = now.AddDays(-day + 3)
            };
            _db.Shipments.Add(shipment);
        }

        // -------------------------------------------------------------
        // Stock Adjustments: 1 Draft (chờ duyệt kiểm kê) & 2 Approved (đã duyệt)
        // -------------------------------------------------------------
        var adjustmentsCreated = 0;

        // ADJ 1: Draft
        var productAdjDraft1 = products[19];
        var stockDraft1 = existingStocks.FirstOrDefault(s => s.ProductId == productAdjDraft1.Id);
        if (stockDraft1 != null)
        {
            var adjDraft = new StockAdjustment
            {
                Id = Guid.NewGuid(),
                AdjustmentNo = $"ADJ-{now.AddHours(-5):yyyyMMdd}-001",
                Status = StockAdjustmentStatus.Draft,
                Notes = "Kiểm kê định kỳ tháng này - đang đếm thực tế",
                ApprovedById = null,
                ApprovedDate = null,
                Details = new List<StockAdjustmentDetail>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productAdjDraft1.Id,
                        LocationId = stockDraft1.LocationId,
                        CountedQty = stockDraft1.OnhandQty + 2
                    }
                }
            };
            foreach (var d in adjDraft.Details) d.StockAdjustment = adjDraft;
            _db.StockAdjustments.Add(adjDraft);
            adjustmentsCreated++;

            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(StockAdjustment), adjDraft.Id,
                null, nameof(StockAdjustmentStatus.Draft), "Created", hungStaffId, now.AddHours(-5)));
        }

        // ADJ 2 & ADJ 3: Approved
        var approvedAdjSpecs = new[]
        {
            (ProdIdx: 20, Delta: 2, Day: 15, Notes: "Kiểm kê kho TPHCM đợt trước - đã duyệt"),
            (ProdIdx: 21, Delta: -1, Day: 22, Notes: "Kiểm kê kho Hà Nội đợt trước - đã duyệt")
        };

        for (var idx = 0; idx < approvedAdjSpecs.Length; idx++)
        {
            var (prodIdx, delta, day, notes) = approvedAdjSpecs[idx];
            var prod = products[prodIdx];
            var stockEntry = existingStocks.FirstOrDefault(s => s.ProductId == prod.Id);
            if (stockEntry == null) continue;

            var countedQty = Math.Max(0, stockEntry.OnhandQty + delta);
            var actualDelta = countedQty - stockEntry.OnhandQty;
            if (actualDelta == 0) continue;

            var adjNo = $"ADJ-{now.AddDays(-day):yyyyMMdd}-{idx + 2:000}";
            var adj = new StockAdjustment
            {
                Id = Guid.NewGuid(),
                AdjustmentNo = adjNo,
                Status = StockAdjustmentStatus.Approved,
                Notes = notes,
                ApprovedById = manager1Id,
                ApprovedDate = now.AddDays(-day),
                Details = new List<StockAdjustmentDetail>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = prod.Id,
                        LocationId = stockEntry.LocationId,
                        CountedQty = countedQty
                    }
                }
            };
            foreach (var d in adj.Details) d.StockAdjustment = adj;
            _db.StockAdjustments.Add(adj);
            adjustmentsCreated++;

            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(StockAdjustment), adj.Id,
                nameof(StockAdjustmentStatus.Draft), nameof(StockAdjustmentStatus.Approved), "StatusChanged", manager1Id, now.AddDays(-day)));

            var key = (prod.Id, stockEntry.LocationId);
            onhand[key] = countedQty;
            locationQty[stockEntry.LocationId] =
                Math.Max(0, locationQty.GetValueOrDefault(stockEntry.LocationId) + actualDelta);

            var adjMovement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = prod.Id,
                LocationId = stockEntry.LocationId,
                MovementType = MovementType.Adjustment,
                Qty = actualDelta,
                Notes = $"Phiếu điều chỉnh {adjNo} - kiểm kê (delta {actualDelta})"
            };
            _db.StockMovements.Add(adjMovement);
            outboundMovements.Add((adjMovement, now.AddDays(-day)));
        }

        // Cập nhật lại Onhand, Reserved và Location Quantity
        foreach (var ((productId, locId), qty) in onhand)
        {
            var stock = await _db.Stocks.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locId, cancellationToken);
            if (stock != null)
            {
                var tracked = _db.Stocks.Find(stock.Id);
                if (tracked != null)
                {
                    tracked.OnhandQty = qty;
                    tracked.ReservedQty = reserved.GetValueOrDefault((productId, locId));
                }
            }
        }
        foreach (var (locId, qty) in locationQty)
        {
            var loc = await _db.Locations.FindAsync(new object[] { locId }, cancellationToken);
            if (loc != null) loc.CurrentQuantity = qty;
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var (movement, occurred) in outboundMovements)
        {
            await _db.StockMovements
                .Where(x => x.Id == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreatedDate, occurred)
                    .SetProperty(x => x.CreatedById, hungStaffId == Guid.Empty ? null : (Guid?)hungStaffId),
                    cancellationToken);
            await _db.AuditLogs
                .Where(a => a.EntityType == nameof(StockMovement) && a.EntityId == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.OccurredAtUtc, occurred)
                    .SetProperty(a => a.ActorUserId, hungStaffId == Guid.Empty ? null : (Guid?)hungStaffId),
                    cancellationToken);
        }

        var totalSoCount = 7;
        _logger.LogInformation("Multi-status Outbound seeded: SOs (New, Allocated, Picking, Packed, Shipped), Pickings (Open, Assigned, InProgress, Completed), Shipments, Adjustments (Draft, Approved).");

        return (totalSoCount, adjustmentsCreated);
    }

    private Location? FindPutAwayLocation(Guid productId, int qty, IReadOnlyDictionary<Guid, int> locationQty)
    {
        var stockLocations = _db.Locations.AsNoTracking()
            .Where(l => l.LocationType == LocationType.Storage)
            .OrderBy(l => l.WarehouseId).ThenBy(l => l.Code)
            .ToList();

        var productStockLoc = _db.Stocks.AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(s => s.LocationId)
            .ToList();
        var preferred = stockLocations.FirstOrDefault(l => productStockLoc.Contains(l.Id));
        if (preferred != null && locationQty.GetValueOrDefault(preferred.Id) + qty <= preferred.MaxQuantity)
            return preferred;

        return stockLocations
            .Where(l => locationQty.GetValueOrDefault(l.Id) + qty <= l.MaxQuantity)
            .OrderBy(l => locationQty.GetValueOrDefault(l.Id))
            .FirstOrDefault();
    }

    private Location? FindStockLocation(Guid productId, IReadOnlyDictionary<(Guid, Guid), int> onhand)
    {
        var locations = _db.Locations.AsNoTracking()
            .Where(l => l.LocationType == LocationType.Storage)
            .OrderBy(l => l.WarehouseId).ThenBy(l => l.Code)
            .ToList();
        var withStock = locations.FirstOrDefault(l => onhand.ContainsKey((productId, l.Id)));
        return withStock ?? locations.FirstOrDefault();
    }
}
