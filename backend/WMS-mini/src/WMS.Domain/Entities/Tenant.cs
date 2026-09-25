using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    public bool HasExpiryManagement { get; set; } = false;

    public bool IsActive { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? VerifiedDate { get; set; }
}
