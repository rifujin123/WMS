using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class StockAdjustmentDetail : BaseAuditableEntity
{
    public Guid StockAdjustmentId { get; set; }

    [Required]
    public StockAdjustment StockAdjustment { get; set; } = null!;

    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public Guid LocationId { get; set; }

    [Required]
    public Location Location { get; set; } = null!;

    public int CountedQty { get; set; }
}