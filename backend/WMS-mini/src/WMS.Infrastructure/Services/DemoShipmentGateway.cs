using Microsoft.Extensions.Logging;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

/// <summary>
/// Demo seam cho tích hợp vận chuyển: chỉ ghi log.
/// Khi làm tích hợp 3rd-party, thay bằng implementation gọi carrier thật
/// (tạo đơn vận chuyển, lấy tracking, webhook trạng thái...).
/// </summary>
public class DemoShipmentGateway : IShipmentGateway
{
    private readonly ILogger<DemoShipmentGateway> _logger;

    public DemoShipmentGateway(ILogger<DemoShipmentGateway> logger)
    {
        _logger = logger;
    }

    public Task NotifyShippedAsync(ShipmentShippedNotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DemoShipmentGateway] Shipment {ShipmentId} for SaleOrder {SaleOrderNo} marked shipped at {ShippedDateUtc} UTC.",
            notification.ShipmentId,
            notification.SaleOrderNo,
            notification.ShippedDateUtc);
        return Task.CompletedTask;
    }
}