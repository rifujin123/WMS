using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class PutAwayTask : BaseAuditableEntity
{
    public Guid ReceivingDetailId { get; set; }

    [Required]
    public ReceivingDetail ReceivingDetail { get; set; } = null!;

    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public Guid? FromLocationId { get; set; }
    public Location? FromLocation { get; set; }
    public Guid? ToLocationId { get; set; }
    public Location? ToLocation { get; set; }
    public PutAwayTaskStatus Status { get; set; }
    public Guid? AssignToId { get; set; }
    public User? AssignTo { get; set; }
    public Guid? AssignedById { get; set; }
    public User? AssignedBy { get; set; }
    public DateTime? AssignedDate { get; set; }
    public Guid? StartedById { get; set; }
    public User? StartedBy { get; set; }
    public DateTime? StartedDate { get; set; }
    public Guid? CompletedById { get; set; }
    public User? CompletedBy { get; set; }
    public DateTime? CompletedDate { get; set; }
}
