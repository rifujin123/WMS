using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

/// Seed dữ liệu demo thật (điện thoại / laptop / phụ kiện).
/// Ticket 01: scaffold + gating (Seed:Enabled) + base idempotency + logging.
/// Các nhóm dữ liệu (users, warehouse, stock, document…) được các ticket 02–08 điền vào đây.
public class DemoDataSeeder : IDemoDataSeeder
{
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

        // Ticket 02–08: lần lượt seed users, warehouses+locations, categories+products,
        // vendors+customers, stock+movements, và các chuỗi document mẫu. Điền counts vào summary.
        var summary = new SeedSummary();

        _logger.LogInformation(
            "Demo data seeding completed (warehouses={Warehouses}, locations={Locations}, products={Products}).",
            summary.Warehouses, summary.Locations, summary.Products);

        return summary;
    }
}