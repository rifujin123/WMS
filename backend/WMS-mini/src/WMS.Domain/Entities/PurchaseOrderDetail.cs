using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class PurchaseOrderDetail : BaseAuditableEntity
{
    public Guid PurchaseOrderId { get; set; }

    [Required]
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
}
