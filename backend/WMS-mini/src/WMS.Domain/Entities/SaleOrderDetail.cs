using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class SaleOrderDetail : BaseAuditableEntity
{
    public Guid SaleOrderId { get; set; }

    [Required]
    public SaleOrder SaleOrder { get; set; } = null!;

    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public int AllocatedQty { get; set; }
    public SaleOrderDetailStatus Status { get; set; }

    public ICollection<PickingDetail> PickingDetails { get; set; } = new List<PickingDetail>();
}
