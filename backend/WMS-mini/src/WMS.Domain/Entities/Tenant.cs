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

    // Tùy biến thương hiệu (White-label Branding)
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? LoginBackgroundUrl { get; set; }

    [MaxLength(200)]
    public string? FooterText { get; set; }

    // Tích hợp AI Bring-Your-Own-Key (BYOK)
    [MaxLength(255)]
    public string? AiApiKey { get; set; }

    [MaxLength(100)]
    public string? AiModel { get; set; } = "gemini-1.5-flash";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? VerifiedDate { get; set; }
}
