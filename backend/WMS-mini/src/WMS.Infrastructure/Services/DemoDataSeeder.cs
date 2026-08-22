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

/// Seed dá»¯ liá»‡u demo tháº­t (Ä‘iá»‡n thoáº¡i / laptop / phá»¥ kiá»‡n).
/// Ticket 01: scaffold + gating (Seed:Enabled) + base idempotency + logging.
/// Ticket 02: seed users & roles (2 manager + 4 staff), idempotent theo username.
/// CÃ¡c nhÃ³m dá»¯ liá»‡u cÃ²n láº¡i Ä‘Æ°á»£c cÃ¡c ticket 03â€“08 Ä‘iá»n vÃ o Ä‘Ã¢y.
public class DemoDataSeeder : IDemoDataSeeder
{
    private static readonly string[] DemoRoles = { "Admin", "WarehouseManager", "WarehouseStaff" };

    // (code, name, address, aisles, racks, levels) â€” HCM 4Ã—3Ã—2=24, HN 5Ã—4Ã—2=40
    private static readonly (string Code, string Name, string Address, int Aisles, int Racks, int Levels)[] DemoWarehouses =
    {
        ("WH-HCM", "Kho TPHCM", "12 Nguyá»…n VÄƒn Linh, PhÆ°á»ng TÃ¢n PhÃº, Quáº­n 7, TP.HCM", 4, 3, 2),
        ("WH-HN",  "Kho HÃ  Ná»™i", "Sá»‘ 8 LÃª Quang Äáº¡o, PhÆ°á»ng Má»¹ ÄÃ¬nh, Nam Tá»« LiÃªm, HÃ  Ná»™i", 5, 4, 2)
    };

    private static readonly string[] DemoCategories =
    {
        "Äiá»‡n thoáº¡i", "Laptop", "Phá»¥ kiá»‡n", "MÃ¡y tÃ­nh báº£ng"
    };

    // (category, sku, name, price VND, unit, dimension) â€” 60 sáº£n pháº©m thá»‹ trÆ°á»ng VN.
    private static readonly (string Category, string Sku, string Name, decimal Price, string Unit, string Dimension)[] DemoProducts =
    {
        // Äiá»‡n thoáº¡i (18)
        ("Äiá»‡n thoáº¡i", "IP15PM-256-TT",  "iPhone 15 Pro Max 256GB Titan Tá»± NhiÃªn", 31_990_000, "Chiáº¿c", "15.9x7.7x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "IP15PM-512-NT",  "iPhone 15 Pro Max 512GB Titan Xanh",     37_990_000, "Chiáº¿c", "15.9x7.7x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "IP15P-128-TD",   "iPhone 15 Pro 128GB Titan Äen",          27_990_000, "Chiáº¿c", "14.6x7.1x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "IP14-128-XD",    "iPhone 14 128GB Xanh DÆ°Æ¡ng",             20_490_000, "Chiáº¿c", "14.7x7.2x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "IP13-128-H",     "iPhone 13 128GB Há»“ng",                   15_990_000, "Chiáº¿c", "14.7x7.2x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "SS-S24U-256-D",  "Samsung Galaxy S24 Ultra 256GB Äen",     28_990_000, "Chiáº¿c", "16.2x7.9x0.9 cm"),
        ("Äiá»‡n thoáº¡i", "SS-S24-256-T",   "Samsung Galaxy S24 256GB Titan",          21_990_000, "Chiáº¿c", "14.7x7.1x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "SS-S23FE-256-X", "Samsung Galaxy S23 FE 256GB Xanh",       12_990_000, "Chiáº¿c", "15.8x7.6x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "SS-A55-128-X",   "Samsung Galaxy A55 5G 128GB Xanh",        8_490_000, "Chiáº¿c", "16.1x7.7x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "XM-14-256-D",    "Xiaomi 14 256GB Äen",                   18_990_000, "Chiáº¿c", "15.3x7.2x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "XM-RN13-128-X",  "Xiaomi Redmi Note 13 128GB Xanh",        4_990_000, "Chiáº¿c", "16.2x7.5x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "XM-RA3-64-D",    "Xiaomi Redmi A3 64GB Äen",              2_490_000, "Chiáº¿c", "16.3x7.6x0.9 cm"),
        ("Äiá»‡n thoáº¡i", "OP-R11-256-X",   "OPPO Reno 11 256GB Xanh",               10_990_000, "Chiáº¿c", "16.2x7.4x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "OP-A58-128-D",   "OPPO A58 128GB Äen",                     4_690_000, "Chiáº¿c", "16.6x7.6x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "VV-V30-256-X",   "Vivo V30 256GB Xanh",                   12_990_000, "Chiáº¿c", "16.4x7.5x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "GP-P8-128-O",    "Google Pixel 8 128GB Oáº£i hÆ°Æ¡ng",        19_990_000, "Chiáº¿c", "15.1x7.1x0.9 cm"),
        ("Äiá»‡n thoáº¡i", "RM-12P-256-X",   "Realme 12 Pro 256GB Xanh",              9_990_000, "Chiáº¿c", "16.2x7.4x0.8 cm"),
        ("Äiá»‡n thoáº¡i", "NK-G42-128-D",   "Nokia G42 128GB Äen",                    3_290_000, "Chiáº¿c", "16.5x7.6x0.9 cm"),
        // Laptop (16)
        ("Laptop", "MBP14-M3-512-X",     "MacBook Pro 14 M3 512GB XÃ¡m",           38_990_000, "Chiáº¿c", "31.3x22.1x1.6 cm"),
        ("Laptop", "MBA13-M2-256-X",     "MacBook Air 13 M2 256GB XÃ¡m",           22_990_000, "Chiáº¿c", "30.4x21.5x1.1 cm"),
        ("Laptop", "MBP16-M3P-1T-NT",    "MacBook Pro 16 M3 Pro 1TB Äen",         62_990_000, "Chiáº¿c", "35.6x24.8x1.7 cm"),
        ("Laptop", "DELL-XPS13-16-512-D", "Dell XPS 13 i7 16GB/512GB Äen",        30_990_000, "Chiáº¿c", "29.5x19.9x1.5 cm"),
        ("Laptop", "DELL-IN15-8-512-D",  "Dell Inspiron 15 i5 8GB/512GB Äen",     15_990_000, "Chiáº¿c", "35.9x23.5x1.8 cm"),
        ("Laptop", "LEN-TP14-16-512-D",  "Lenovo ThinkPad E14 i5 16GB/512GB Äen", 18_990_000, "Chiáº¿c", "32.5x23.6x1.8 cm"),
        ("Laptop", "LEN-IP5-16-512-X",   "Lenovo IdeaPad Slim 5 i5 16GB/512GB XÃ¡m",16_990_000, "Chiáº¿c", "35.6x23.7x1.7 cm"),
        ("Laptop", "HP-PV15-8-512-D",    "HP Pavilion 15 i5 8GB/512GB Äen",       15_490_000, "Chiáº¿c", "35.9x23.6x1.8 cm"),
        ("Laptop", "HP-EB840-16-512-D",  "HP EliteBook 840 G10 i7 16GB/512GB Äen",28_990_000, "Chiáº¿c", "31.6x22.4x1.9 cm"),
        ("Laptop", "ASUS-VB15-8-256-D",  "ASUS VivoBook 15 i3 8GB/256GB Äen",     10_990_000, "Chiáº¿c", "35.9x23.6x1.9 cm"),
        ("Laptop", "ASUS-G16-16-512-D",  "ASUS ROG Strix G16 i7 16GB/512GB Äen",  38_990_000, "Chiáº¿c", "35.4x26.4x2.3 cm"),
        ("Laptop", "ACER-AS5-8-512-D",   "Acer Aspire 5 i5 8GB/512GB Äen",        13_990_000, "Chiáº¿c", "36.3x23.8x1.8 cm"),
        ("Laptop", "ACER-N5-16-512-D",   "Acer Nitro 5 i5 16GB/512GB Äen",        20_990_000, "Chiáº¿c", "36.0x27.1x2.5 cm"),
        ("Laptop", "MS-SL5-8-256-D",     "Microsoft Surface Laptop 5 i5 8GB/256GB Äen", 26_990_000, "Chiáº¿c", "32.3x22.3x1.5 cm"),
        ("Laptop", "SS-GB3P-16-512-X",   "Samsung Galaxy Book3 Pro 16GB/512GB XÃ¡m", 29_990_000, "Chiáº¿c", "35.6x25.3x1.2 cm"),
        ("Laptop", "XIAO-RB16-16-512-X", "Xiaomi RedmiBook 16 i5 16GB/512GB XÃ¡m", 14_990_000, "Chiáº¿c", "36.1x23.5x1.6 cm"),
        // Phá»¥ kiá»‡n (14)
        ("Phá»¥ kiá»‡n", "AP-AP2-W",         "AirPods Pro 2",                           5_490_000, "Bá»™",   "2.4x2.2x3.1 cm"),
        ("Phá»¥ kiá»‡n", "AP-AP3-W",         "AirPods 3",                              3_990_000, "Bá»™",   "3.0x1.9x1.9 cm"),
        ("Phá»¥ kiá»‡n", "SS-BUDS2-W",       "Samsung Galaxy Buds2 Pro",               3_490_000, "Bá»™",   "2.2x2.2x1.9 cm"),
        ("Phá»¥ kiá»‡n", "SS-BUDSFE-W",      "Samsung Galaxy Buds FE",                 1_690_000, "Bá»™",   "2.0x1.8x2.4 cm"),
        ("Phá»¥ kiá»‡n", "XM-BUD4-W",        "Xiaomi Buds 4 Pro",                      1_990_000, "Bá»™",   "2.5x2.0x2.0 cm"),
        ("Phá»¥ kiá»‡n", "AP-WSE44-M",       "Apple Watch SE 44mm",                    7_490_000, "Chiáº¿c", "4.4x3.8x1.0 cm"),
        ("Phá»¥ kiá»‡n", "AP-WS9-M",         "Apple Watch Series 9 45mm",             11_990_000, "Chiáº¿c", "4.5x3.8x1.0 cm"),
        ("Phá»¥ kiá»‡n", "ANK-65G-D",        "Sáº¡c GaN Anker 65W",                       890_000, "Chiáº¿c", "7.2x3.6x3.0 cm"),
        ("Phá»¥ kiá»‡n", "ANK-PB20-D",       "Sáº¡c dá»± phÃ²ng Anker 20000mAh",            1_290_000, "Chiáº¿c", "15.2x7.1x2.6 cm"),
        ("Phá»¥ kiá»‡n", "LOG-MX3S-G",       "Chuá»™t Logitech MX Master 3S",            2_490_000, "Chiáº¿c", "5.1x12.4x4.3 cm"),
        ("Phá»¥ kiá»‡n", "LOG-K380-W",       "BÃ n phÃ­m Logitech K380",                  890_000, "Chiáº¿c", "27.9x12.4x1.6 cm"),
        ("Phá»¥ kiá»‡n", "LOG-G502-D",       "Chuá»™t gaming Logitech G502 Hero",        1_190_000, "Chiáº¿c", "7.5x13.2x4.0 cm"),
        ("Phá»¥ kiá»‡n", "AP-MK-W",          "Apple Magic Keyboard",                   2_990_000, "Chiáº¿c", "27.9x11.5x0.4 cm"),
        ("Phá»¥ kiá»‡n", "SS-T7-1T-D",       "SSD Samsung T7 1TB",                     2_990_000, "Chiáº¿c", "8.5x5.7x0.8 cm"),
        // MÃ¡y tÃ­nh báº£ng (12)
        ("MÃ¡y tÃ­nh báº£ng", "IPD-PRO11-M4-S", "iPad Pro 11 M4 256GB XÃ¡m",          28_990_000, "Chiáº¿c", "24.9x17.8x0.6 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "IPD-AIR13-M2-S", "iPad Air 13 M2 256GB XÃ¡m",          21_990_000, "Chiáº¿c", "28.1x21.5x0.6 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "IPD-10-64-S",    "iPad 10 64GB Xanh",                 10_990_000, "Chiáº¿c", "24.9x17.5x0.7 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "IPD-MINI6-P",    "iPad mini 6 64GB Há»“ng",              12_990_000, "Chiáº¿c", "19.5x13.5x0.6 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "SS-TABS9-X",     "Samsung Galaxy Tab S9 256GB Xanh",   19_990_000, "Chiáº¿c", "25.4x16.6x0.6 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "SS-TABA9-X",     "Samsung Galaxy Tab A9+ 64GB Xanh",    5_990_000, "Chiáº¿c", "25.7x16.8x0.7 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "XM-PAD6-D",      "Xiaomi Pad 6 128GB Äen",             8_990_000, "Chiáº¿c", "25.4x16.5x0.6 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "LEN-TABP12-X",   "Lenovo Tab P12 128GB XÃ¡m",           7_990_000, "Chiáº¿c", "29.4x18.1x0.7 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "OP-PAD-D",       "OPPO Pad Air 128GB XÃ¡m",              6_990_000, "Chiáº¿c", "25.0x16.4x0.7 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "VIVO-PAD2-X",    "Vivo Pad2 128GB Xanh",               9_490_000, "Chiáº¿c", "26.7x17.4x0.7 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "HUAWEI-M11-G",   "Huawei MatePad 11 128GB XÃ¡m",       8_490_000, "Chiáº¿c", "25.4x16.2x0.7 cm"),
        ("MÃ¡y tÃ­nh báº£ng", "ACER-TAB10-D",   "Acer Iconia Tab 10 64GB Äen",        3_990_000, "Chiáº¿c", "24.7x15.6x0.8 cm"),
    };

    // (name, contactName, phone, email, address) â€” nhÃ  phÃ¢n phá»‘i á»§y quyá»n + Ä‘áº¡i lÃ½ VN.
    private static readonly (string Name, string ContactName, string Phone, string Email, string Address)[] DemoVendors =
    {
        ("CÃ´ng ty TNHH Tháº¿ Giá»›i Di Äá»™ng",   "Nguyá»…n VÄƒn Nam",   "028 3838 5000", "contact@thegioididong.vn", "60-62 LÃª Lá»£i, Q.1, TPHCM"),
        ("CÃ´ng ty Cá»• pháº§n FPT Trading",     "Tráº§n Thanh Háº£i",   "024 3726 5555", "sale@fpttrading.com.vn",    "Sá»‘ 10A LÃ½ ThÆ°á»ng Kiá»‡t, Q.HoÃ n Kiáº¿m, HÃ  Ná»™i"),
        ("CÃ´ng ty TNHH Samsung Electronics VN", "LÃª Thá»‹ HÆ°Æ¡ng",  "028 3622 8080", "hcm@samsung.com.vn",      "Sá»‘ 2 ÄÆ°á»ng 3/2, Quáº­n 10, TPHCM"),
        ("CÃ´ng ty TNHH Apple Authorized Reseller VN", "Pháº¡m Minh ChÃ¢u", "024 3733 8080", "sale@apple-reseller.vn", "25 ÄÆ°á»ng LÃ¡ng, Q.Äá»‘ng Äa, HÃ  Ná»™i"),
        ("CÃ´ng ty TNHH ThÆ°Æ¡ng máº¡i & Dá»‹ch vá»¥ Ká»¹ thuáº­t Long", "Nguyá»…n Quá»‘c Long", "028 3999 1234", "info@longtech.com.vn", "88 Nguyá»…n TrÃ£i, Q.Thanh XuÃ¢n, HÃ  Ná»™i"),
        ("CÃ´ng ty Cá»• pháº§n Sáº£n xuáº¥t & TM PhÃ¡t Äáº¡t", "HoÃ ng VÄƒn PhÃ¡t", "024 3356 7890", "phatdat@phatdat.vn", "45 Tráº§n HÆ°ng Äáº¡o, Q.HoÃ n Kiáº¿m, HÃ  Ná»™i"),
        ("CÃ´ng ty TNHH Anpha Computer",     "VÃµ An Khoa",       "028 3766 4321", "sales@anphacom.vn",       "120 Nguyá»…n ChÃ­ Thanh, Q.5, TPHCM"),
        ("CÃ´ng ty TNHH MTV Äiá»‡n tá»­ Gia Báº£o", "ÄoÃ n Thá»‹ Mai",     "024 3868 2468", "giabao@giabao.vn",        "68 Quang Trung, Q.Hai BÃ  TrÆ°ng, HÃ  Ná»™i"),
        ("CÃ´ng ty TNHH Netco Viá»‡t Nam",     "Trá»‹nh Báº£o Khang",  "028 3822 1999", "netco@netco.vn",          "33 Nguyá»…n VÄƒn Cá»«, Q.1, TPHCM"),
        ("CÃ´ng ty Cá»• pháº§n E-phone (ENS)",   "Kiá»u Háº¡nh Nhi",    "024 3560 8899", "ens@e-phone.vn",          "22 LÃª Thanh Nghá»‹, Q.Hai BÃ  TrÆ°ng, HÃ  Ná»™i"),
    };

    // (name, contactName, phone, email, address) â€” doanh nghiá»‡p + khÃ¡ch láº», sá»“/thiáº¿u email/address á»Ÿ khÃ¡ch láº».
    private static readonly (string Name, string ContactName, string Phone, string Email, string Address)[] DemoCustomers =
    {
        ("CÃ´ng ty CP Äáº§u tÆ° Minh Anh",      "BÃ¹i Thá»‹ Há»“ng Nhung", "0912 345 678", "minhanh@minhanh.com.vn",   "12 Nguyá»…n ÄÃ¬nh Chiá»ƒu, Q.1, TPHCM"),
        ("CÃ´ng ty TNHH ThÆ°Æ¡ng máº¡i HoÃ ng Gia","Phan Tháº¿ HoÃ ng",    "0903 123 456", "hoanggia@hoanggia.vn",     "56 LÃª VÄƒn Sá»¹, Q.TÃ¢n BÃ¬nh, TPHCM"),
        ("CÃ´ng ty Cá»• pháº§n Dá»‡t may Viá»‡t Tháº¯ng", "LÃª Minh Äá»©c",    "0988 765 432", "vietthang@vietthang.vn",   "78 Nguyá»…n TrÃ£i, Q.Thanh XuÃ¢n, HÃ  Ná»™i"),
        ("CÃ´ng ty TNHH XÃ¢y dá»±ng Nam Long",  "Nguyá»…n ThÃ nh Long", "0909 111 222", "namlong@namlong.vn",       "90 Láº¡c Long QuÃ¢n, Q.TÃ¢y Há»“, HÃ  Ná»™i"),
        ("CÃ´ng ty Cá»• pháº§n GiÃ¡o dá»¥c Vina",   "Tráº§n Thu Trang",    "0913 555 666", "vinaedu@vinaedu.vn",       "33 Tráº§n Quá»‘c VÆ°á»£ng, Q.Cáº§u Giáº¥y, HÃ  Ná»™i"),
        ("CÃ´ng ty TNHH Dá»‹ch vá»¥ Sáº¯c MÃ u",    "Há»“ Ngá»c Ãnh",       "0933 777 888", "sacmau@sacmau.vn",         "18 Pháº¡m VÄƒn Äá»“ng, Q.GÃ² Váº¥p, TPHCM"),
        ("CÃ´ng ty CP CÃ´ng nghá»‡ BÃ¡ch Khoa",  "VÅ© Huy CÆ°á»ng",      "0916 234 567", "bachkhoa@bk-tech.vn",      "101 Tráº§n HÆ°ng Äáº¡o, Q.5, TPHCM"),
        ("CÃ´ng ty TNHH Kinh doanh ASA",     "Äá»— ThuÃ½ HÃ ",        "0977 888 999", "asa@asa.vn",               "67 HÃ ng BÃ i, Q.HoÃ n Kiáº¿m, HÃ  Ná»™i"),
        ("CÃ´ng ty Cá»• pháº§n ThÃ©p Miá»n Nam",   "LÆ°Æ¡ng VÄƒn TÃ i",     "0989 123 321", "thp@thepmienam.vn",        "24 TÃ´n Äá»©c Tháº¯ng, Q.1, TPHCM"),
        ("CÃ´ng ty TNHH Truyá»n thÃ´ng SkyMedia", "NgÃ´ Thanh Thuá»·", "0917 456 789", "skymedia@skymedia.vn",     "44 Pháº¡m Ngá»c Tháº¡ch, Q.Äá»‘ng Äa, HÃ  Ná»™i"),
        ("CÃ´ng ty Cá»• pháº§n Sá»¯a BÃ¬nh Minh",   "Äinh Thá»‹ Háº¡nh",     "0902 333 444", "binhminh@bms.vn",          "9 Giáº£ng VÃµ, Q.Ba ÄÃ¬nh, HÃ  Ná»™i"),
        ("CÃ´ng ty TNHH DÆ°á»£c pháº©m PhÃº Gia",  "TrÆ°Æ¡ng VÄƒn PhÃºc",   "0935 222 111", "phugia@phugia.vn",         "55 HoÃ ng Hoa ThÃ¡m, Q.Ba ÄÃ¬nh, HÃ  Ná»™i"),
        ("CÃ´ng ty Cá»• pháº§n Logistics NorthStar", "Mai HoÃ ng DÅ©ng", "0944 567 890", "northstar@nslog.vn",       "77 Nguyá»…n Há»¯u Thá», Q.7, TPHCM"),
        ("KhÃ¡ch láº» - Anh Tuáº¥n",             "Nguyá»…n Anh Tuáº¥n",   "0908 333 555", "",                        ""),
        ("KhÃ¡ch láº» - Chá»‹ Lan",              "Tráº§n Thá»‹ Lan",      "0914 666 777", "",                        ""),
        ("KhÃ¡ch láº» - Anh Minh",             "LÃª Quang Minh",     "0932 444 555", "",                        ""),
        ("KhÃ¡ch láº» - Chá»‹ Hoa",              "Pháº¡m Thu Hoa",      "0905 222 888", "",                        ""),
        ("KhÃ¡ch láº» - Anh Khoa",             "Äinh VÄƒn Khoa",     "0981 777 123", "",                        ""),
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

        // Base idempotency: náº¿u dá»¯ liá»‡u demo Ä‘Ã£ tá»“n táº¡i (kho Ä‘Ã£ cÃ³) thÃ¬ skip toÃ n bá»™
        // Ä‘á»ƒ trÃ¡nh duplicate khi cháº¡y láº¡i / cháº¡y giá»¯a chá»«ng sau má»™t Ä‘á»£t seed trÆ°á»›c Ä‘Ã³.
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
        // StockMovements = tá»•ng cáº£ ticket 06 (ledger) + ticket 07 (putaway In movements) tá»« DB.
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
            "Demo data seeding completed (users={Users}, warehouses={Warehouses}, locations={Locations}, categories={Categories}, products={Products}, vendors={Vendors}, customers={Customers}, stockMovements={StockMovements}, purchaseOrders={PurchaseOrders}).",
            summary.Users, summary.Warehouses, summary.Locations, summary.Categories, summary.Products, summary.Vendors, summary.Customers, summary.StockMovements, summary.PurchaseOrders);

        return summary;
    }

    private async Task<int> SeedUsersAsync(CancellationToken cancellationToken)
    {
        // Äáº£m báº£o roles tá»“n táº¡i trÆ°á»›c khi gÃ¡n user (khÃ´ng táº¡o kÃ©p).
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
            ("manager1", "Nguyá»…n VÄƒn An",     "manager1@wms.local",     "WarehouseManager"),
            ("manager2", "Tráº§n Thá»‹ BÃ¬nh",     "manager2@wms.local",     "WarehouseManager"),
            // Staff
            ("nvhung",   "Nguyá»…n VÄƒn HÃ¹ng",   "nvhung@wms.local",       "WarehouseStaff"),
            ("nvlan",    "LÃª Thá»‹ Lan",        "nvlan@wms.local",        "WarehouseStaff"),
            ("pvnam",    "Pháº¡m VÄƒn Nam",      "pvnam@wms.local",        "WarehouseStaff"),
            ("nthao",    "Nguyá»…n Thá»‹ Tháº£o",   "nthao@wms.local",        "WarehouseStaff"),
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

    // Kiá»ƒu + dung lÆ°á»£ng tá»‘i Ä‘a theo vá»‹ trÃ­ (deterministic theo thá»© tá»± sinh).
    // Æ¯u tiÃªn Storage; vÃ i vá»‹ trÃ­ Ä‘áº§u lÃ m Receiving / Shipping / Picking cho há»£p nghiá»‡p vá»¥.
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

    // áº¢nh placeholder stable theo SKU: cÃ¹ng SKU â†’ cÃ¹ng áº£nh (picsum.photos seed).
    // Cháº¥p nháº­n phá»¥ thuá»™c internet khi demo; khÃ´ng dÃ¹ng Cloudinary trong scope nÃ y.
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

            // Deterministic ledger: In trÆ°á»›c rá»“i Out/Adjustment â€” khÃ´ng bao giá» tá»“n Ã¢m
            // vÃ¬ running balance chá»‰ giáº£m sau khi Ä‘Ã£ cÃ³ Ä‘á»§ In.
            // Ranges Ä‘Æ°á»£c giá»¯ nhá» Ä‘á»ƒ 2 sáº£n pháº©m dÃ¹ng chung 1 storage slot váº«n â‰¤ MaxQuantity (200).
            var finalOnhand = 10 + ((i * 13) % 40);          // 10..49
            var soldTotal = 15 + ((i * 7) % 30);             // 15..44
            var adjustTotal = 1 + ((i * 5) % 8);             // 1..8
            var totalIn = soldTotal + adjustTotal + finalOnhand;

            var suffix = $"{product.Sku} @ {location.Code}";
            var seq = new (int DayOffset, MovementType Type, int Qty, string Note)[]
            {
                (-42, MovementType.In, totalIn / 2, $"Nháº­n hÃ ng nháº­p kho {suffix}"),
                (-35, MovementType.In, totalIn - totalIn / 2, $"Nháº­n hÃ ng nháº­p kho {suffix}"),
                (-28, MovementType.Out, soldTotal / 3, $"BÃ¡n hÃ ng - xuáº¥t kho {suffix}"),
                (-20, MovementType.Adjustment, adjustTotal, $"Kiá»ƒm kÃª - Ä‘iá»u chá»‰nh tá»“n {suffix}"),
                (-10, MovementType.Out, soldTotal / 3, $"BÃ¡n hÃ ng - xuáº¥t kho {suffix}"),
                (-3,  MovementType.Out, soldTotal - 2 * (soldTotal / 3), $"BÃ¡n hÃ ng - xuáº¥t kho {suffix}"),
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

            // Onhand = running balance tháº­t qua toÃ n bá»™ chuá»—i ledger
            // (= Î£ In âˆ’ Î£ Out + Î£ Adjustment), consistency by construction.
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

        // Backdate CreatedDate + CreatedById (interceptor Ã©p = now khi Add).
        // ExecuteUpdate khÃ´ng qua ChangeTracker â†’ khÃ´ng sinh thÃªm audit kÃ©p.
        foreach (var movement in movements)
        {
            var occurred = movementTimestamps[movement.Id];
            await _db.StockMovements
                .Where(x => x.Id == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreatedDate, occurred)
                    .SetProperty(x => x.CreatedById, actorId == Guid.Empty ? null : (Guid?)actorId),
                    cancellationToken);

            // Backdate luÃ´n AuditLog tá»± sinh cho movement (interceptor Ã©p OccurredAtUtc = now).
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

    private async Task<int> SeedPoAndPutAwayChainsAsync(CancellationToken cancellationToken)
    {
        var vendors = await _db.Vendors.AsNoTracking().ToListAsync(cancellationToken);
        var products = await _db.Products.AsNoTracking().ToListAsync(cancellationToken);
        var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken);
        var managerId = users.FirstOrDefault(u => u.NormalizedUserName == "MANAGER1")?.Id ?? Guid.Empty;
        var staffId = users.FirstOrDefault(u => u.NormalizedUserName == "NVHUNG")?.Id ?? Guid.Empty;

        if (vendors.Count < 3 || products.Count < 6)
            return 0;

        // Load tráº¡ng thÃ¡i tá»“n + vá»‹ trÃ­ hiá»‡n táº¡i (sau ticket 06) Ä‘á»ƒ cáº­p nháº­t nháº¥t quÃ¡n.
        var existingStocks = await _db.Stocks.AsNoTracking().ToListAsync(cancellationToken);
        var onhand = existingStocks.ToDictionary(s => (s.ProductId, s.LocationId), s => s.OnhandQty);
        var locationQty = (await _db.Locations.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(l => l.Id, l => l.CurrentQuantity);

        var now = DateTime.UtcNow;
        var chains = new[]
        {
            (VendorIndex: 0, ProductIndexes: new[] { 0, 1 }, QtyPer: 12, Day: 38),
            (VendorIndex: 1, ProductIndexes: new[] { 2, 3 }, QtyPer: 10, Day: 30),
            (VendorIndex: 2, ProductIndexes: new[] { 4, 5 }, QtyPer: 8,  Day: 24),
        };

        var purchaseOrders = 0;
        var putAwayMovements = new List<(StockMovement Movement, DateTime Occurred)>();
        foreach (var (vendorIndex, productIndexes, qtyPer, day) in chains)
        {
            var vendor = vendors[vendorIndex];
            var usedProducts = productIndexes.Select(i => products[i]).ToList();

            // PO: Pending â†’ Approved â†’ Received â†’ Closed (final = Closed).
            // LÆ°u Ã½: khÃ´ng táº¡o PurchaseOrderDetail trong seed â€” entity Ä‘Ã³ cÃ³ RowVersion (IsRowVersion,
            // store-generated) khÃ´ng thá»ƒ insert qua EF trÃªn SQLite (test seam); flow sáº£n pháº©m váº«n
            // Ä‘Æ°á»£c thá»ƒ hiá»‡n qua ReceivingDetail + PutAwayTask. TrÃªn SQL Server production, cÃ¡c thao tÃ¡c
            // nghiá»‡p vá»¥ sau nÃ y sáº½ táº¡o detail nhÆ° bÃ¬nh thÆ°á»ng.
            var po = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                PoNumber = $"PO-{now.AddDays(-day):yyyyMMdd}-{purchaseOrders + 1:000}",
                VendorName = vendor.Name,
                Status = PurchaseOrderStatus.Closed,
                ApprovedById = managerId,
                ApprovedDate = now.AddDays(-day),
                ClosedById = managerId,
                ClosedDate = now.AddDays(-3)
            };

            // Receiving: Draft â†’ Confirmed (final = Confirmed), 1 per PO.
            var receiving = new Receiving
            {
                Id = Guid.NewGuid(),
                ReceivingNo = $"RC-{now.AddDays(-day):yyyyMMdd}-{purchaseOrders + 1:000}",
                PurchaseOrderId = po.Id,
                PurchaseOrder = po,
                Status = ReceivingStatus.Confirmed,
                ReceivedById = staffId,
                ReceivedDate = now.AddDays(-20),
                ConfirmedById = managerId,
                ConfirmedDate = now.AddDays(-19),
                Notes = $"Nháº­n hÃ ng theo {po.PoNumber}",
                ReceivingDetails = usedProducts.Select(p => new ReceivingDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.Id,
                    ExpectedQuantity = qtyPer,
                    ActualQuantity = qtyPer,
                    Condition = ProductCondition.Ok
                }).ToList()
            };

            // Link FK
            foreach (var d in receiving.ReceivingDetails) d.Receiving = receiving;

            // PutAway: cho má»—i dÃ²ng Ok â†’ Completed; cá»™ng tá»“n táº¡i chÃ­nh location áº¥y.
            foreach (var detail in receiving.ReceivingDetails)
            {
                var targetLocation = FindPutAwayLocation(detail.ProductId, detail.ActualQuantity, locationQty);
                if (targetLocation is null) continue;

                var before = onhand.GetValueOrDefault((detail.ProductId, targetLocation.Id));
                onhand[(detail.ProductId, targetLocation.Id)] = before + detail.ActualQuantity;
                locationQty[targetLocation.Id] = locationQty.GetValueOrDefault(targetLocation.Id) + detail.ActualQuantity;

                var putAway = new PutAwayTask
                {
                    Id = Guid.NewGuid(),
                    ReceivingDetailId = detail.Id,
                    ReceivingDetail = detail,
                    ProductId = detail.ProductId,
                    Quantity = detail.ActualQuantity,
                    FromLocationId = null,
                    ToLocationId = targetLocation.Id,
                    Status = PutAwayTaskStatus.Completed,
                    AssignToId = staffId,
                    AssignedById = managerId,
                    AssignedDate = now.AddDays(-18),
                    StartedById = staffId,
                    StartedDate = now.AddDays(-17),
                    CompletedById = staffId,
                    CompletedDate = now.AddDays(-16),
                };
                _db.PutAwayTasks.Add(putAway);

                var putAwayOccurred = now.AddDays(-16);
                var putAwayMovement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = detail.ProductId,
                    LocationId = targetLocation.Id,
                    MovementType = MovementType.In,
                    Qty = detail.ActualQuantity,
                    Notes = $"Phiáº¿u cáº¥t {po.PoNumber} - nháº­p kho {targetLocation.Code}"
                };
                _db.StockMovements.Add(putAwayMovement);
                putAwayMovements.Add((putAwayMovement, putAwayOccurred));
            }

            // StatusHistory cho PO: Pendingâ†’Approved, Approvedâ†’Received, Receivedâ†’Closed.
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po.Id,
                nameof(PurchaseOrderStatus.Pending), nameof(PurchaseOrderStatus.Approved), "StatusChanged",
                managerId, now.AddDays(-day)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po.Id,
                nameof(PurchaseOrderStatus.Approved), nameof(PurchaseOrderStatus.Received), "StatusChanged",
                staffId, now.AddDays(-19)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(PurchaseOrder), po.Id,
                nameof(PurchaseOrderStatus.Received), nameof(PurchaseOrderStatus.Closed), "StatusChanged",
                managerId, now.AddDays(-3)));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Receiving), receiving.Id,
                nameof(ReceivingStatus.Draft), nameof(ReceivingStatus.Confirmed), "StatusChanged",
                managerId, now.AddDays(-19)));

            _db.PurchaseOrders.Add(po);
            _db.Receivings.Add(receiving);
            purchaseOrders++;
        }

        // Ghi láº¡i onhand/location vÃ o DB (Stock + Location Ä‘Ã£ track; cáº­p nháº­t luÃ´n).
        foreach (var ((productId, locationId), qty) in onhand)
        {
            var stock = await _db.Stocks.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locationId, cancellationToken);
            if (stock != null)
            {
                var tracked = _db.Stocks.Find(stock.Id);
                if (tracked != null)
                    tracked.OnhandQty = qty;
            }
        }
        foreach (var (locationId, qty) in locationQty)
        {
            var loc = await _db.Locations.FindAsync(new object[] { locationId }, cancellationToken);
            if (loc != null) loc.CurrentQuantity = qty;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Backdate putaway movements + audit logs tá»± sinh (giá»‘ng ticket 06), Ä‘á»ƒ cá»­a sá»• lá»‹ch sá»­ thá»‘ng nháº¥t.
        foreach (var (movement, occurred) in putAwayMovements)
        {
            await _db.StockMovements
                .Where(x => x.Id == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreatedDate, occurred)
                    .SetProperty(x => x.CreatedById, staffId == Guid.Empty ? null : (Guid?)staffId),
                    cancellationToken);
            await _db.AuditLogs
                .Where(a => a.EntityType == nameof(StockMovement) && a.EntityId == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.OccurredAtUtc, occurred)
                    .SetProperty(a => a.ActorUserId, staffId == Guid.Empty ? null : (Guid?)staffId),
                    cancellationToken);
        }

        _logger.LogInformation("Demo PO chains seeded: {Count} purchase orders (all Closed, 1 Confirmed receiving each, PutAway completed).", purchaseOrders);
        return purchaseOrders;
    }

    // Chá»n location nháº­n hÃ ng cáº¥t: Æ°u tiÃªn location sáºµn cÃ³ stock cá»§a sáº£n pháº©m (Ä‘Ã£ náº¡p tá»« ticket 06),
    // ngÆ°á»£c láº¡i dÃ¹ng location rá»—ng nháº¥t, giá»¯ capacity â‰¤ MaxQuantity.
    private Location? FindPutAwayLocation(Guid productId, int qty, IReadOnlyDictionary<Guid, int> locationQty)
    {
        var stockLocations = _db.Locations.AsNoTracking()
            .Where(l => l.LocationType == LocationType.Storage)
            .OrderBy(l => l.WarehouseId).ThenBy(l => l.Code)
            .ToList();

        // Æ¯u tiÃªn storage location cá»§a sáº£n pháº©m Ä‘Ã£ tá»“n (xÃ¡c Ä‘á»‹nh láº¡i tá»« Stocks).
        var productStockLoc = _db.Stocks.AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(s => s.LocationId)
            .ToList();
        var preferred = stockLocations.FirstOrDefault(l => productStockLoc.Contains(l.Id));
        if (preferred != null && locationQty.GetValueOrDefault(preferred.Id) + qty <= preferred.MaxQuantity)
            return preferred;

        // Fallback: location rá»—ng nháº¥t cÃ³ Ä‘á»§ chá»—.
        return stockLocations
            .Where(l => locationQty.GetValueOrDefault(l.Id) + qty <= l.MaxQuantity)
            .OrderBy(l => locationQty.GetValueOrDefault(l.Id))
            .FirstOrDefault();
    }

    private async Task<(int SaleOrders, int StockAdjustments)> SeedSaleOrderPickingAndAdjustmentAsync(CancellationToken cancellationToken)
    {
        var customers = await _db.Customers.AsNoTracking().ToListAsync(cancellationToken);
        var products = await _db.Products.AsNoTracking().ToListAsync(cancellationToken);
        var warehouses = await _db.Warehouses.AsNoTracking().ToListAsync(cancellationToken);
        var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken);
        var staffId = users.FirstOrDefault(u => u.NormalizedUserName == "NVHUNG")?.Id ?? Guid.Empty;
        var managerId = users.FirstOrDefault(u => u.NormalizedUserName == "MANAGER1")?.Id ?? Guid.Empty;

        if (customers.Count < 2 || products.Count < 6 || warehouses.Count == 0)
            return (0, 0);

        // Load tráº¡ng thÃ¡i tá»“n + vá»‹ trÃ­ hiá»‡n táº¡i (sau tickets 06-07).
        var existingStocks = await _db.Stocks.AsNoTracking().ToListAsync(cancellationToken);
        var onhand = existingStocks.ToDictionary(s => (s.ProductId, s.LocationId), s => s.OnhandQty);
        var stocksByKey = existingStocks.ToDictionary(s => (s.ProductId, s.LocationId), s => s);
        var locationQty = (await _db.Locations.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(l => l.Id, l => l.CurrentQuantity);

        var now = DateTime.UtcNow;
        var orderSpecs = new[]
        {
            (CustomerIndex: 0, ProductIndexes: new[] { 6, 7 }, QtyPer: 3),
            (CustomerIndex: 1, ProductIndexes: new[] { 8, 9 }, QtyPer: 5),
            (CustomerIndex: 2, ProductIndexes: new[] { 10, 11 }, QtyPer: 2),
        };

        var saleOrdersCreated = 0;
        var outboundMovements = new List<(StockMovement Movement, DateTime Occurred)>();
        for (var idx = 0; idx < orderSpecs.Length; idx++)
        {
            var (customerIndex, productIndexes, qtyPer) = orderSpecs[idx];
            var customer = customers[customerIndex];
            var usedProducts = productIndexes.Select(i => products[i]).ToList();
            var warehouse = warehouses[0];

            var saleOrder = new SaleOrder
            {
                Id = Guid.NewGuid(),
                OrderNo = $"SO-{now.AddDays(-(16 - idx * 5)):yyyyMMdd}-{idx + 1:000}",
                CustomerName = customer.Name,
                OrderDate = now.AddDays(-(16 - idx * 5)),
                Status = SaleOrderStatus.Packed,
                PackedById = staffId,
                PackedDate = now.AddDays(-(4 + idx)),
                SaleOrderDetails = usedProducts.Select(p => new SaleOrderDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.Id, // chá»‰ FK â€” Product lÃ  master Ä‘Ã£ track, khÃ´ng gÃ¡n navigation
                    Quantity = qtyPer,
                    AllocatedQty = qtyPer,
                    Status = SaleOrderDetailStatus.Picked
                }).ToList()
            };
            foreach (var d in saleOrder.SaleOrderDetails) d.SaleOrder = saleOrder;

            // Picking: Open â†’ Assigned â†’ InProgress â†’ Completed. QtyPicked == QtyToPick.
            var picking = new Picking
            {
                Id = Guid.NewGuid(),
                PickingNo = $"PK-{now.AddDays(-(16 - idx * 5)):yyyyMMdd}-{idx + 1:000}",
                WarehouseId = warehouse.Id, // chá»‰ FK â€” Warehouse Ä‘Ã£ track
                Status = PickingStatus.Completed,
                AssignedToId = staffId,
                AssignedById = managerId,
                AssignedDate = now.AddDays(-(14 - idx * 5)),
                StartedById = staffId,
                StartedDate = now.AddDays(-(13 - idx * 5)),
                CompletedById = staffId,
                CompletedDate = now.AddDays(-(6 + idx)),
                PickingDetails = saleOrder.SaleOrderDetails.Select(d => new PickingDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = d.ProductId, // chá»‰ FK
                    SaleOrderDetailId = d.Id,
                    SaleOrderDetail = d, // SaleOrderDetail má»›i cÃ¹ng chain â€” ok
                    QtyToPick = d.Quantity,
                    QtyPicked = d.Quantity,
                    Status = PickingDetailStatus.Picked,
                    LocationId = FindStockLocation(d.ProductId, onhand)?.Id // chá»‰ FK
                }).ToList()
            };
            foreach (var pd in picking.PickingDetails) pd.Picking = picking;

            // Picking hoÃ n thÃ nh â†’ trá»« tá»“n + ghi Out movement.
            foreach (var pd in picking.PickingDetails)
            {
                if (pd.LocationId is not Guid locationId) continue;
                var key = (pd.ProductId, locationId);
                var before = onhand.GetValueOrDefault(key);
                var after = Math.Max(0, before - pd.QtyPicked);
                onhand[key] = after;
                locationQty[locationId] = Math.Max(0, locationQty.GetValueOrDefault(locationId) - pd.QtyPicked);

                var occurred = now.AddDays(-(6 + idx));
                var movement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = pd.ProductId,
                    LocationId = locationId,
                    MovementType = MovementType.Out,
                    Qty = pd.QtyPicked,
                    Notes = $"Phiáº¿u láº¥y hÃ ng {picking.PickingNo} - xuáº¥t kho"
                };
                _db.StockMovements.Add(movement);
                outboundMovements.Add((movement, occurred));
            }

            // StatusHistory cho SaleOrder + Picking.
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), saleOrder.Id,
                nameof(SaleOrderStatus.New), nameof(SaleOrderStatus.Allocated), "StatusChanged",
                staffId, now.AddDays(-(15 - idx * 5))));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), saleOrder.Id,
                nameof(SaleOrderStatus.Allocated), nameof(SaleOrderStatus.Picking), "StatusChanged",
                staffId, now.AddDays(-(14 - idx * 5))));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(SaleOrder), saleOrder.Id,
                nameof(SaleOrderStatus.Picking), nameof(SaleOrderStatus.Packed), "StatusChanged",
                staffId, now.AddDays(-(4 + idx))));
            _db.StatusHistories.Add(new StatusHistory(Guid.NewGuid(), nameof(Picking), picking.Id,
                nameof(PickingStatus.Open), nameof(PickingStatus.Completed), "StatusChanged",
                staffId, now.AddDays(-(6 + idx))));

            _db.SaleOrders.Add(saleOrder);
            _db.Pickings.Add(picking);
            saleOrdersCreated++;
        }

        // StockAdjustments: 2 phiáº¿u Draft â†’ Approved; CountedQty chÃªnh lá»‡ch thá»±c â†’ Adjustment movement.
        var adjustmentsCreated = 0;
        for (var idx = 0; idx < 2; idx++)
        {
            var product = products[12 + idx]; // sáº£n pháº©m phá»¥ kiá»‡n/phá»¥ tÃ¹ng
            var stockEntry = existingStocks.FirstOrDefault(s => s.ProductId == product.Id);
            if (stockEntry == null) continue;

            var adjustmentNo = $"ADJ-{now.AddDays(-8 + idx * 2):yyyyMMdd}-{idx + 1:000}";
            // CountedQty kiá»ƒm kÃª thá»±c: chÃªnh lá»‡ch nhá» so vá»›i onhand (cá»™ng/trá»« vÃ i Ä‘Æ¡n vá»‹).
            var countedQty = Math.Max(0, stockEntry.OnhandQty + (idx == 0 ? 2 : -1));
            var delta = countedQty - stockEntry.OnhandQty;
            if (delta == 0) continue;

            var adjustment = new StockAdjustment
            {
                Id = Guid.NewGuid(),
                AdjustmentNo = adjustmentNo,
                Status = StockAdjustmentStatus.Approved,
                Notes = "Kiá»ƒm kÃª Ä‘á»‹nh ká»³ - Ä‘iá»u chá»‰nh tá»“n kho",
                ApprovedById = managerId,
                ApprovedDate = now.AddDays(-8 + idx * 2),
                Details = new List<StockAdjustmentDetail>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        LocationId = stockEntry.LocationId,
                        CountedQty = countedQty
                    }
                }
            };
            foreach (var d in adjustment.Details) d.StockAdjustment = adjustment;
            _db.StockAdjustments.Add(adjustment);
            adjustmentsCreated++;

            var key = (product.Id, stockEntry.LocationId);
            onhand[key] = countedQty;
            locationQty[stockEntry.LocationId] =
                Math.Max(0, locationQty.GetValueOrDefault(stockEntry.LocationId) + delta);

            var adjOccurred = now.AddDays(-8 + idx * 2);
            var adjMovement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                LocationId = stockEntry.LocationId,
                MovementType = MovementType.Adjustment,
                Qty = delta, // signed delta (giá»‘ng ApproveAsync) â€” cÃ³ thá»ƒ Ã¢m
                Notes = $"Phiáº¿u Ä‘iá»u chá»‰nh {adjustmentNo} - kiá»ƒm kÃª (delta {delta})"
            };
            _db.StockMovements.Add(adjMovement);
            outboundMovements.Add((adjMovement, adjOccurred));
        }

        // Ghi láº¡i onhand/location vÃ o DB.
        foreach (var ((productId, locationId), qty) in onhand)
        {
            var stock = await _db.Stocks.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locationId, cancellationToken);
            if (stock != null)
            {
                var tracked = _db.Stocks.Find(stock.Id);
                if (tracked != null) tracked.OnhandQty = qty;
            }
        }
        foreach (var (locationId, qty) in locationQty)
        {
            var loc = await _db.Locations.FindAsync(new object[] { locationId }, cancellationToken);
            if (loc != null) loc.CurrentQuantity = qty;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Backdate picking movements + auto audit logs.
        foreach (var (movement, occurred) in outboundMovements)
        {
            await _db.StockMovements
                .Where(x => x.Id == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreatedDate, occurred)
                    .SetProperty(x => x.CreatedById, staffId == Guid.Empty ? null : (Guid?)staffId),
                    cancellationToken);
            await _db.AuditLogs
                .Where(a => a.EntityType == nameof(StockMovement) && a.EntityId == movement.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.OccurredAtUtc, occurred)
                    .SetProperty(a => a.ActorUserId, staffId == Guid.Empty ? null : (Guid?)staffId),
                    cancellationToken);
        }

        _logger.LogInformation("Demo outbound seeded: {Orders} sale orders (Packed), {Adjustments} adjustments (Approved).",
            saleOrdersCreated, adjustmentsCreated);
        return (saleOrdersCreated, adjustmentsCreated);
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