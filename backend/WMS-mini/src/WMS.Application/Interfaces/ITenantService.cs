using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface ITenantService
{
    Task RegisterTenantAsync(RegisterTenantDto dto, string? clientOrigin = null);
    Task VerifyEmailAsync(VerifyEmailDto dto);
}
