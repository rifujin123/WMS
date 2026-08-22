using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

/// Hợp đồng seed dữ liệu demo: chạy idempotent (chạy lại không duplicate),
/// được gate bằng Seed:Enabled. Gọi 1 lần sau Migrate trong Development.
public interface IDemoDataSeeder
{
    Task<SeedSummary> SeedAsync(CancellationToken cancellationToken = default);
}