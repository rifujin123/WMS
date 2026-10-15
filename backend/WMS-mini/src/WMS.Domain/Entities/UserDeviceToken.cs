using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class UserDeviceToken
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(500)]
    public string FcmToken { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DeviceType { get; set; } = "Web";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
