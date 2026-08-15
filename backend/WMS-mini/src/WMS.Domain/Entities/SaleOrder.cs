using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class SaleOrder : BaseAuditableEntity
{
    [MaxLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CustomerName { get; set; }

    public DateTime OrderDate { get; set; }
    public SaleOrderStatus Status { get; set; }
    public Guid? PackedById { get; set; }
    public User? PackedBy { get; set; }
    public DateTime? PackedDate { get; set; }

    public ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();
    public Shipment? Shipment { get; set; }
    public ICollection<Rma> Rmas { get; set; } = new List<Rma>();
}
