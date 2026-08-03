using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class StockAdjustment : BaseAuditableEntity
{
    [MaxLength(50)]
    public string AdjustmentNo { get; set; } = string.Empty;

    public StockAdjustmentStatus Status { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<StockAdjustmentDetail> Details { get; set; } = new List<StockAdjustmentDetail>();
}