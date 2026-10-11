using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class TenantConfigDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public bool HasExpiryManagement { get; set; }
    public string? LogoUrl { get; set; }
    public string? LoginBackgroundUrl { get; set; }
    public string? FooterText { get; set; }
    public bool HasAiKeyConfigured { get; set; }
    public string? MaskedAiApiKey { get; set; }
    public string? AiModel { get; set; }
}

public class UpdateTenantConfigDto
{
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? LoginBackgroundUrl { get; set; }

    [MaxLength(200)]
    public string? FooterText { get; set; }

    [MaxLength(255)]
    public string? AiApiKey { get; set; }

    [MaxLength(100)]
    public string? AiModel { get; set; }
}

public class TestAiKeyDto
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}

public class TestAiKeyResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class PublicTenantBrandingDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? LoginBackgroundUrl { get; set; }
    public string? FooterText { get; set; }
}
