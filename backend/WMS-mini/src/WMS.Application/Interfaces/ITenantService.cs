using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface ITenantService
{
    Task RegisterTenantAsync(RegisterTenantDto dto, string? clientOrigin = null);
    Task VerifyEmailAsync(VerifyEmailDto dto);
    Task<TenantConfigDto> GetCurrentTenantConfigAsync();
    Task UpdateTenantConfigAsync(UpdateTenantConfigDto dto);
    Task<TestAiKeyResultDto> TestAiKeyAsync(TestAiKeyDto dto);
    Task<PublicTenantBrandingDto?> GetPublicBrandingAsync(string tenantCode);
    Task<bool> TestEmailConnectionAsync(string testToEmail);
}
