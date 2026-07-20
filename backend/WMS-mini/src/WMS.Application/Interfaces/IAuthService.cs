using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task RegisterAsync(RegisterDto dto);
}
