namespace WMS.Application.DTOs;

/// <summary>
/// Payload báo lô hàng đã giao cho gateway vận chuyển bên thứ ba.
/// </summary>
public class ShipmentShippedNotification
{
    public Guid ShipmentId { get; set; }
    public Guid SaleOrderId { get; set; }
    public string? SaleOrderNo { get; set; }
    public DateTime ShippedDateUtc { get; set; }
}