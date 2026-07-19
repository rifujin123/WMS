using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Location : BaseAuditableEntity
{
    public Guid WarehouseId { get; set; }

    [Required]
    public Warehouse Warehouse { get; set; } = null!;

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Aisle { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Rack { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Level { get; set; } = string.Empty;

    public LocationType LocationType { get; set; }
    public int MaxQuantity { get; set; }
    public int CurrentQuantity { get; set; }
}
