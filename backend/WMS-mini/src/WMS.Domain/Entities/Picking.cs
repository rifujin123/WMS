using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Picking : BaseAuditableEntity
{
    public Guid WarehouseId { get; set; }

    [Required]
    public Warehouse Warehouse { get; set; } = null!;

    public PickingStatus Status { get; set; }
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public ICollection<PickingDetail> PickingDetails { get; set; } = new List<PickingDetail>();
}
