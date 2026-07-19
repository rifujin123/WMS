using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class PickingDetail : BaseAuditableEntity
{
    public Guid PickingId { get; set; }

    [Required]
    public Picking Picking { get; set; } = null!;

    public Guid? SaleOrderDetailId { get; set; }
    public SaleOrderDetail? SaleOrderDetail { get; set; }

    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public int QtyToPick { get; set; }
    public int QtyPicked { get; set; }
    public PickingDetailStatus Status { get; set; }
}
