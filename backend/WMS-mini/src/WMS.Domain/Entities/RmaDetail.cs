using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class RmaDetail : BaseAuditableEntity
{
    public Guid RmaId { get; set; }

    [Required]
    public Rma Rma { get; set; } = null!;

    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public ProductCondition Condition { get; set; }
    public Disposition Disposition { get; set; }
}
