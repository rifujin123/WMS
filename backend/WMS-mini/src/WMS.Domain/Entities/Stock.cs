using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Stock : BaseAuditableEntity
{
    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public Guid LocationId { get; set; }

    [Required]
    public Location Location { get; set; } = null!;

    public int OnhandQty { get; set; }
    public int ReservedQty { get; set; }
}
