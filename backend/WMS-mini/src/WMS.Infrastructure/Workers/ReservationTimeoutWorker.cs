using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Workers;

public class ReservationTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationTimeoutWorker> _logger;
    private readonly TimeSpan _checkInterval;

    public ReservationTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationTimeoutWorker> logger,
        TimeSpan? checkInterval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationTimeoutWorker started with interval {Interval}.", _checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình xử lý đơn đặt cọc quá hạn.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ReservationTimeoutWorker stopped.");
    }

    public async Task<int> ProcessExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        var now = DateTime.UtcNow;
        var expiredOrders = await db.SaleOrders
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                && s.Status == SaleOrderStatus.AwaitingDeposit
                && s.DepositDeadline.HasValue
                && s.DepositDeadline.Value <= now)
            .Include(s => s.SaleOrderDetails)
            .ToListAsync(cancellationToken);

        if (expiredOrders.Count == 0)
            return 0;

        _logger.LogInformation("Phát hiện {Count} đơn bán hàng quá hạn nộp cọc. Đang tiến hành hủy tự động...", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            order.Status = SaleOrderStatus.Cancelled;
            order.UpdatedDate = now;

            const string cancelReason = "[Tự động hủy: Quá hạn nộp tiền cọc]";
            order.DepositNotes = string.IsNullOrWhiteSpace(order.DepositNotes)
                ? cancelReason
                : $"{order.DepositNotes} | {cancelReason}";

            var detailIds = order.SaleOrderDetails.Select(d => d.Id).ToList();
            if (detailIds.Count > 0)
            {
                var links = await db.BackorderLinks
                    .IgnoreQueryFilters()
                    .Where(b => !b.IsDeleted && detailIds.Contains(b.SaleOrderDetailId))
                    .ToListAsync(cancellationToken);

                if (links.Count > 0)
                {
                    db.BackorderLinks.RemoveRange(links);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã hủy tự động {Count} đơn bán hàng quá hạn nộp cọc thành công.", expiredOrders.Count);
        return expiredOrders.Count;
    }
}
