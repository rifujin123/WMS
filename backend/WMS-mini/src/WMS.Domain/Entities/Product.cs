using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Product : BaseAuditableEntity
{
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    [Required]
    public Category Category { get; set; } = null!;

    [MaxLength(50)]
    public string? Unit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(100)]
    public string? Dimension { get; set; }
}
