using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class BackorderLink : BaseAuditableEntity
{
    public Guid SaleOrderDetailId { get; set; }

    [Required]
    public SaleOrderDetail SaleOrderDetail { get; set; } = null!;

    public Guid PurchaseOrderDetailId { get; set; }

    [Required]
    public PurchaseOrderDetail PurchaseOrderDetail { get; set; } = null!;

    public int Quantity { get; set; }
    public int FulfilledQuantity { get; set; }
    public bool IsFulfilled { get; set; }
}
