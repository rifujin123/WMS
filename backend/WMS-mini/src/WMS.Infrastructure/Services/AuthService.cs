using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly WmsDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AuthService(
        UserManager<User> userManager,
        IConfiguration configuration,
        WmsDbContext db,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var input = dto.Username?.Trim() ?? string.Empty;
        var user = await _userManager.FindByNameAsync(input)
            ?? await _userManager.FindByEmailAsync(input);

        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu không chính xác.");

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant != null && !tenant.IsActive)
        {
            throw new UnauthorizedAccessException("Tài khoản doanh nghiệp chưa được kích hoạt. Vui lòng xác thực email trước khi đăng nhập.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles, tenant);

        return new AuthResponseDto
        {
            AccessToken = token,
            RefreshToken = string.Empty,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl
        };
    }

    public async Task RegisterAsync(RegisterDto dto)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Không thể xác định doanh nghiệp của người dùng hiện tại.");
        }

        if (dto.WarehouseId.HasValue)
        {
            var warehouseExists = await _db.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId.Value);
            if (!warehouseExists)
            {
                throw new InvalidOperationException("Kho được chọn không tồn tại hoặc không thuộc quyền quản lý của doanh nghiệp bạn.");
            }
        }

        var user = new User
        {
            UserName = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            AvatarUrl = dto.AvatarUrl,
            WarehouseId = dto.WarehouseId,
            TenantId = tenantId.Value,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        var role = string.IsNullOrWhiteSpace(dto.Role) ? "WarehouseStaff" : dto.Role;
        if (role != "Admin" && role != "WarehouseManager" && role != "WarehouseStaff")
            throw new Exception($"Invalid role '{role}'.");

        if ((role == "WarehouseStaff" || role == "WarehouseManager") && !dto.WarehouseId.HasValue)
        {
            throw new InvalidOperationException("Nhân viên kho và Quản lý kho bắt buộc phải được gán vào một kho cụ thể.");
        }

        await _userManager.AddToRoleAsync(user, role);
    }

    private string GenerateJwtToken(User user, IList<string> roles, Tenant? tenant)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", user.TenantId.ToString())
        };

        if (tenant != null)
        {
            claims.Add(new Claim("has_expiry_management", tenant.HasExpiryManagement.ToString().ToLowerInvariant()));
            claims.Add(new Claim("tenant_code", tenant.Code));
        }

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        if (user.WarehouseId.HasValue)
        {
            claims.Add(new Claim("warehouseId", user.WarehouseId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
