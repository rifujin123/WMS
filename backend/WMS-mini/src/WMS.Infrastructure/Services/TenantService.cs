using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly WmsDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        WmsDbContext db,
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<TenantService> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task RegisterTenantAsync(RegisterTenantDto dto, string? clientOrigin = null)
    {
        var cleanCode = dto.CompanyCode.Trim().ToLowerInvariant();
        var cleanEmail = dto.AdminEmail.Trim().ToLowerInvariant();

        // 1. Kiểm tra mã doanh nghiệp duy nhất
        var codeExists = await _db.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Code == cleanCode);

        if (codeExists)
        {
            throw new InvalidOperationException("Mã doanh nghiệp đã tồn tại trên hệ thống. Vui lòng chọn mã khác.");
        }

        // 2. Kiểm tra email đại diện duy nhất toàn hệ thống
        var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email này đã được sử dụng. Vui lòng sử dụng email khác.");
        }

        // 3. Khởi tạo Tenant (ở trạng thái chưa kích hoạt)
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = dto.CompanyName.Trim(),
            Code = cleanCode,
            Address = dto.Address?.Trim(),
            ContactPhone = dto.ContactPhone?.Trim(),
            ContactEmail = cleanEmail,
            HasExpiryManagement = dto.HasExpiryManagement,
            IsActive = false,
            CreatedDate = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        // 4. Tạo tài khoản Tenant Admin
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = cleanEmail,
            Email = cleanEmail,
            FullName = dto.AdminFullName.Trim(),
            TenantId = tenant.Id,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(adminUser, dto.Password);
        if (!createResult.Succeeded)
        {
            // Rollback tenant nếu tạo user thất bại
            _db.Tenants.Remove(tenant);
            await _db.SaveChangesAsync();
            var error = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Không thể tạo tài khoản quản trị: {error}");
        }

        // 5. Gán Role Admin
        await _userManager.AddToRoleAsync(adminUser, "Admin");

        // 6. Tạo Token xác thực email & Gửi mail
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(adminUser);
        var origin = !string.IsNullOrWhiteSpace(clientOrigin) ? clientOrigin : "http://localhost:5173";
        var verificationUrl = $"{origin.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(cleanEmail)}";

        await _emailService.SendVerificationEmailAsync(cleanEmail, adminUser.FullName, verificationUrl);

        _logger.LogInformation("Đã tiếp nhận đăng ký tenant '{TenantCode}' ({TenantId}) cho admin '{AdminEmail}'.", tenant.Code, tenant.Id, cleanEmail);
    }

    public async Task VerifyEmailAsync(VerifyEmailDto dto)
    {
        var cleanEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(cleanEmail);
        if (user == null)
        {
            throw new InvalidOperationException("Không tìm thấy tài khoản tương ứng với email này.");
        }

        // Xác nhận token qua ASP.NET Identity
        var confirmResult = await _userManager.ConfirmEmailAsync(user, dto.Token);
        if (!confirmResult.Succeeded)
        {
            throw new InvalidOperationException("Liên kết xác thực không hợp lệ hoặc đã hết hạn.");
        }

        // Kích hoạt Tenant
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant != null && !tenant.IsActive)
        {
            tenant.IsActive = true;
            tenant.VerifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Tenant '{TenantCode}' ({TenantId}) đã được kích hoạt thành công qua xác thực email.", tenant.Code, tenant.Id);
        }
    }
}
