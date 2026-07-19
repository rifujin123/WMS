using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class StockMovement : BaseAuditableEntity
{
    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public Guid LocationId { get; set; }

    [Required]
    public Location Location { get; set; } = null!;

    public MovementType MovementType { get; set; }
    public int Qty { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
