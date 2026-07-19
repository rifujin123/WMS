using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Shipment : BaseAuditableEntity
{
    public Guid SaleOrderId { get; set; }

    [Required]
    public SaleOrder SaleOrder { get; set; } = null!;

    [MaxLength(100)]
    public string? Carrier { get; set; }

    [MaxLength(100)]
    public string? TrackingNo { get; set; }

    public DateTime? ShippedDate { get; set; }
}
