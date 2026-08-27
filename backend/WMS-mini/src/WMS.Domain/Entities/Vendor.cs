using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Vendor : BaseAuditableEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}