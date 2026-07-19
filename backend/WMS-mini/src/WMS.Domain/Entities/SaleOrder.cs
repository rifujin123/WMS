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

    public ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<Rma> Rmas { get; set; } = new List<Rma>();
}
