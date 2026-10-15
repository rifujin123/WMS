using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
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
        var emailService = scope.ServiceProvider.GetService<IEmailService>();

        var now = DateTime.UtcNow;
        var expiredOrders = await db.SaleOrders
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                && s.Status == SaleOrderStatus.AwaitingDeposit
                && s.DepositDeadline.HasValue
                && s.DepositDeadline.Value <= now)
            .Include(s => s.SaleOrderDetails)
            .Include(s => s.Customer)
            .ToListAsync(cancellationToken);

        if (expiredOrders.Count == 0)
            return 0;

        _logger.LogInformation("Phát hiện {Count} đơn bán hàng quá hạn nộp cọc. Đang tiến hành hủy tự động...", expiredOrders.Count);

        var ordersToAlert = new List<SaleOrder>();

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

            ordersToAlert.Add(order);
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã hủy tự động {Count} đơn bán hàng quá hạn nộp cọc thành công.", expiredOrders.Count);

        // Safe Non-blocking: Gửi email cảnh báo sau khi transaction DB đã commit
        if (emailService != null && ordersToAlert.Count > 0)
        {
            await DispatchCancellationAlertEmailsAsync(db, emailService, ordersToAlert, cancellationToken);
        }

        return expiredOrders.Count;
    }

    private async Task DispatchCancellationAlertEmailsAsync(
        WmsDbContext db,
        IEmailService emailService,
        List<SaleOrder> orders,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantIds = orders.Select(o => o.TenantId).Distinct().ToList();
            var tenants = await db.Tenants
                .IgnoreQueryFilters()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

            foreach (var order in orders)
            {
                if (!tenants.TryGetValue(order.TenantId, out var tenant))
                    continue;

                if (!tenant.EnableEmailAlerts || string.IsNullOrWhiteSpace(tenant.AlertEmailRecipients))
                    continue;

                var recipients = tenant.AlertEmailRecipients
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(e => !string.IsNullOrWhiteSpace(e) && e.Contains('@'))
                    .Distinct()
                    .ToList();

                if (recipients.Count == 0)
                    continue;

                var customerName = order.Customer?.Name ?? "Khách vãng lai";
                var subject = $"[Cảnh báo Hủy đơn] Đơn hàng {order.OrderNo} đã bị tự động hủy do quá hạn cọc";
                var htmlBody = BuildCancellationAlertHtml(order, customerName, tenant.Name);

                foreach (var recipient in recipients)
                {
                    try
                    {
                        await emailService.SendAlertEmailAsync(recipient, "Ban Quản Trị / Kế Toán", subject, htmlBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể gửi email cảnh báo hủy đơn {OrderNo} tới {Recipient}: {Message}",
                            order.OrderNo, recipient, ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong tiến trình gửi email cảnh báo tự động của ReservationTimeoutWorker.");
        }
    }

    private static string BuildCancellationAlertHtml(SaleOrder order, string customerName, string companyName)
    {
        var itemCount = order.SaleOrderDetails?.Sum(d => d.Quantity) ?? 0;
        var totalAmount = order.TotalAmount.ToString("N0") + " đ";
        var depositAmount = order.RequiredDeposit.ToString("N0") + " đ";

        return $@"
            <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px 16px; background-color: #F8FAFC;"">
                <div style=""background-color: #FFFFFF; border-radius: 8px; border: 1px solid #E2E8F0; padding: 28px 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.04);"">
                    <div style=""margin-bottom: 20px;"">
                        <span style=""display: inline-block; background-color: #EF4444; color: #FFFFFF; font-weight: bold; font-size: 13px; padding: 4px 10px; border-radius: 4px; letter-spacing: 0.5px;"">CẢNH BÁO TỰ ĐỘNG</span>
                    </div>
                    <h2 style=""color: #0F172A; font-size: 20px; font-weight: 700; margin-top: 0; margin-bottom: 12px;"">Đơn bán hàng đã tự động hủy do quá hạn cọc</h2>
                    <p style=""color: #334155; font-size: 14px; line-height: 1.6; margin-bottom: 16px;"">
                        Hệ thống WMS thông báo đơn hàng sau đây đã vượt quá hạn thanh toán tiền cọc và vừa được tự động chuyển sang trạng thái <strong>Đã hủy (Cancelled)</strong> để giải phóng giữ chỗ hàng trong kho:
                    </p>
                    <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 14px;"">
                        <tr style=""border-bottom: 1px solid #F1F5F9;"">
                            <td style=""padding: 8px 0; color: #64748B;"">Mã đơn hàng:</td>
                            <td style=""padding: 8px 0; color: #0F172A; font-weight: 600; font-family: monospace;"">{order.OrderNo}</td>
                        </tr>
                        <tr style=""border-bottom: 1px solid #F1F5F9;"">
                            <td style=""padding: 8px 0; color: #64748B;"">Khách hàng:</td>
                            <td style=""padding: 8px 0; color: #0F172A; font-weight: 500;"">{customerName}</td>
                        </tr>
                        <tr style=""border-bottom: 1px solid #F1F5F9;"">
                            <td style=""padding: 8px 0; color: #64748B;"">Tổng giá trị đơn:</td>
                            <td style=""padding: 8px 0; color: #0F172A; font-weight: 600;"">{totalAmount}</td>
                        </tr>
                        <tr style=""border-bottom: 1px solid #F1F5F9;"">
                            <td style=""padding: 8px 0; color: #64748B;"">Tiền cọc yêu cầu:</td>
                            <td style=""padding: 8px 0; color: #DC2626; font-weight: 600;"">{depositAmount}</td>
                        </tr>
                        <tr style=""border-bottom: 1px solid #F1F5F9;"">
                            <td style=""padding: 8px 0; color: #64748B;"">Tổng số lượng SP giải phóng:</td>
                            <td style=""padding: 8px 0; color: #0F172A; font-weight: 600;"">{itemCount} đơn vị</td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px 0; color: #64748B;"">Hạn nộp cọc ban đầu:</td>
                            <td style=""padding: 8px 0; color: #0F172A;"">{order.DepositDeadline:dd/MM/yyyy HH:mm} UTC</td>
                        </tr>
                    </table>
                    <hr style=""border: none; border-top: 1px solid #E2E8F0; margin: 20px 0;"" />
                    <p style=""color: #94A3B8; font-size: 12px; margin: 0; text-align: center;"">
                        Thông báo tự động từ hệ thống quản lý kho {companyName}. Vui lòng đăng nhập WMS để xem chi tiết.
                    </p>
                </div>
            </div>";
    }
}
