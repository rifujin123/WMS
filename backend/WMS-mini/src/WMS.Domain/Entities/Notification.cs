using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(100)]
    public string? TargetRole { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "System"; // TaskAssigned | ApprovalRequired | Reminder | System

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
