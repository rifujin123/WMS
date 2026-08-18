using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Picking : BaseAuditableEntity
{
    [MaxLength(50)]
    public string PickingNo { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    [Required]
    public Warehouse Warehouse { get; set; } = null!;

    public PickingStatus Status { get; set; }
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    public Guid? AssignedById { get; set; }
    public User? AssignedBy { get; set; }
    public DateTime? AssignedDate { get; set; }
    public Guid? StartedById { get; set; }
    public User? StartedBy { get; set; }
    public DateTime? StartedDate { get; set; }
    public Guid? CompletedById { get; set; }
    public User? CompletedBy { get; set; }
    public DateTime? CompletedDate { get; set; }

    public ICollection<PickingDetail> PickingDetails { get; set; } = new List<PickingDetail>();
}
