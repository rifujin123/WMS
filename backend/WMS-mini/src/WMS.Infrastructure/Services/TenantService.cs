using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        WmsDbContext db,
        UserManager<User> userManager,
        IEmailService emailService,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantService> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task RegisterTenantAsync(RegisterTenantDto dto, string? clientOrigin = null)
    {
        var cleanCode = dto.CompanyCode.Trim().ToLowerInvariant();
        var cleanEmail = dto.AdminEmail.Trim().ToLowerInvariant();

        var codeExists = await _db.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Code == cleanCode);

        if (codeExists)
        {
            throw new InvalidOperationException("Mã doanh nghiệp đã tồn tại trên hệ thống. Vui lòng chọn mã khác.");
        }

        var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email này đã được sử dụng. Vui lòng sử dụng email khác.");
        }

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
            _db.Tenants.Remove(tenant);
            await _db.SaveChangesAsync();
            var error = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Không thể tạo tài khoản quản trị: {error}");
        }

        await _userManager.AddToRoleAsync(adminUser, "Admin");

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

        var confirmResult = await _userManager.ConfirmEmailAsync(user, dto.Token);
        if (!confirmResult.Succeeded)
        {
            throw new InvalidOperationException("Mã xác thực không hợp lệ hoặc đã hết hạn.");
        }

        if (user.TenantId != Guid.Empty)
        {
            var tenant = await _db.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId);

            if (tenant != null)
            {
                tenant.IsActive = true;
                tenant.VerifiedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Tenant '{TenantCode}' ({TenantId}) đã được kích hoạt sau khi xác thực email.", tenant.Code, tenant.Id);
            }
        }
    }

    public async Task<TenantConfigDto> GetCurrentTenantConfigAsync()
    {
        var tenantId = _db.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Không thể xác định không gian doanh nghiệp hiện tại.");
        }

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

        if (tenant == null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin doanh nghiệp.");
        }

        string? maskedKey = null;
        var hasAiKey = !string.IsNullOrWhiteSpace(tenant.AiApiKey);
        if (hasAiKey)
        {
            var key = tenant.AiApiKey!;
            maskedKey = key.Length > 8
                ? $"{key.Substring(0, 6)}...****"
                : "****";
        }

        return new TenantConfigDto
        {
            CompanyName = tenant.Name,
            CompanyCode = tenant.Code,
            Address = tenant.Address,
            ContactPhone = tenant.ContactPhone,
            ContactEmail = tenant.ContactEmail,
            HasExpiryManagement = tenant.HasExpiryManagement,
            LogoUrl = tenant.LogoUrl,
            LoginBackgroundUrl = tenant.LoginBackgroundUrl,
            FooterText = tenant.FooterText,
            HasAiKeyConfigured = hasAiKey,
            MaskedAiApiKey = maskedKey,
            AiModel = tenant.AiModel ?? "gemini-1.5-flash",
            NearExpiryDays = tenant.NearExpiryDays,
            DeadStockDays = tenant.DeadStockDays,
            FrozenCapitalDays = tenant.FrozenCapitalDays,
            EnableExpiryAlertBanner = tenant.EnableExpiryAlertBanner,
            EnableEmailAlerts = tenant.EnableEmailAlerts,
            AlertEmailRecipients = tenant.AlertEmailRecipients
        };
    }

    public async Task UpdateTenantConfigAsync(UpdateTenantConfigDto dto)
    {
        var tenantId = _db.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Không thể xác định không gian doanh nghiệp hiện tại.");
        }

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

        if (tenant == null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin doanh nghiệp.");
        }

        if (!string.IsNullOrWhiteSpace(dto.CompanyName))
            tenant.Name = dto.CompanyName.Trim();

        if (dto.Address != null)
            tenant.Address = dto.Address.Trim();

        if (dto.ContactPhone != null)
            tenant.ContactPhone = dto.ContactPhone.Trim();

        if (dto.LogoUrl != null)
            tenant.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim();

        if (dto.LoginBackgroundUrl != null)
            tenant.LoginBackgroundUrl = string.IsNullOrWhiteSpace(dto.LoginBackgroundUrl) ? null : dto.LoginBackgroundUrl.Trim();

        if (dto.FooterText != null)
            tenant.FooterText = string.IsNullOrWhiteSpace(dto.FooterText) ? null : dto.FooterText.Trim();

        if (dto.AiApiKey != null)
        {
            var trimmedKey = dto.AiApiKey.Trim();
            if (string.IsNullOrEmpty(trimmedKey))
            {
                tenant.AiApiKey = null;
            }
            else if (!trimmedKey.Contains("****"))
            {
                tenant.AiApiKey = trimmedKey;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.AiModel))
            tenant.AiModel = dto.AiModel.Trim();

        if (dto.NearExpiryDays.HasValue)
        {
            if (dto.NearExpiryDays.Value < 1)
                throw new InvalidOperationException("Số ngày cảnh báo cận hạn phải là số nguyên dương lớn hơn hoặc bằng 1.");
            tenant.NearExpiryDays = dto.NearExpiryDays.Value;
        }

        if (dto.DeadStockDays.HasValue)
        {
            if (dto.DeadStockDays.Value < 1)
                throw new InvalidOperationException("Số ngày tính hàng đọng vốn phải là số nguyên dương lớn hơn hoặc bằng 1.");
            tenant.DeadStockDays = dto.DeadStockDays.Value;
        }

        if (dto.FrozenCapitalDays.HasValue)
        {
            if (dto.FrozenCapitalDays.Value < 1)
                throw new InvalidOperationException("Số ngày tính vốn đóng băng phải là số nguyên dương lớn hơn hoặc bằng 1.");
            tenant.FrozenCapitalDays = dto.FrozenCapitalDays.Value;
        }

        if (dto.EnableExpiryAlertBanner.HasValue)
        {
            tenant.EnableExpiryAlertBanner = dto.EnableExpiryAlertBanner.Value;
        }

        if (dto.EnableEmailAlerts.HasValue)
        {
            tenant.EnableEmailAlerts = dto.EnableEmailAlerts.Value;
        }

        if (dto.AlertEmailRecipients != null)
        {
            tenant.AlertEmailRecipients = string.IsNullOrWhiteSpace(dto.AlertEmailRecipients) ? null : dto.AlertEmailRecipients.Trim();
        }

        if (tenant.FrozenCapitalDays > tenant.DeadStockDays)
        {
            throw new InvalidOperationException("Số ngày tính vốn đóng băng không được lớn hơn số ngày tính hàng đọng vốn (Dead Stock).");
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Đã cập nhật cấu hình cho tenant '{TenantCode}' ({TenantId}).", tenant.Code, tenant.Id);
    }

    public async Task<bool> TestEmailConnectionAsync(string testToEmail)
    {
        if (string.IsNullOrWhiteSpace(testToEmail))
        {
            throw new InvalidOperationException("Địa chỉ email kiểm tra không được để trống.");
        }

        return await _emailService.TestEmailConnectionAsync(testToEmail.Trim());
    }

    public async Task<TestAiKeyResultDto> TestAiKeyAsync(TestAiKeyDto dto)
    {
        var tenantId = _db.CurrentTenantId;
        string? candidateKey = dto.ApiKey?.Trim();

        if (string.IsNullOrWhiteSpace(candidateKey) || candidateKey.Contains("****"))
        {
            if (!tenantId.HasValue)
            {
                return new TestAiKeyResultDto
                {
                    Success = false,
                    Message = "Vui lòng nhập API Key để kiểm tra."
                };
            }

            candidateKey = await _db.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Id == tenantId.Value)
                .Select(t => t.AiApiKey)
                .FirstOrDefaultAsync();
        }

        if (string.IsNullOrWhiteSpace(candidateKey))
        {
            return new TestAiKeyResultDto
            {
                Success = false,
                Message = "Chưa có Google Gemini API Key để kiểm tra. Vui lòng nhập khóa API."
            };
        }

        var model = !string.IsNullOrWhiteSpace(dto.Model) ? dto.Model.Trim() : "gemini-1.5-flash";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={candidateKey}";
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = "ping" } }
                    }
                }
            };

            var response = await client.PostAsJsonAsync(endpoint, requestBody);
            if (response.IsSuccessStatusCode)
            {
                return new TestAiKeyResultDto
                {
                    Success = true,
                    Message = $"Kết nối thành công! Khóa API Google Gemini hoạt động ổn định với mô hình '{model}'."
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Kiểm tra Gemini API Key thất bại (HTTP {StatusCode}): {ErrorBody}", response.StatusCode, errorBody);

            return new TestAiKeyResultDto
            {
                Success = false,
                Message = $"Google API từ chối kết nối (Mã lỗi: {(int)response.StatusCode}). Vui lòng kiểm tra lại khóa API hoặc quyền truy cập.",
                Details = errorBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi ngoại lệ khi kiểm tra Google Gemini API Key: {Message}", ex.Message);
            return new TestAiKeyResultDto
            {
                Success = false,
                Message = $"Không thể kết nối đến máy chủ Google: {ex.Message}"
            };
        }
    }

    public async Task<PublicTenantBrandingDto?> GetPublicBrandingAsync(string tenantCode)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return null;

        var cleanCode = tenantCode.Trim().ToLowerInvariant();
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Code == cleanCode && t.IsActive);

        if (tenant == null)
            return null;

        return new PublicTenantBrandingDto
        {
            CompanyName = tenant.Name,
            CompanyCode = tenant.Code,
            LogoUrl = tenant.LogoUrl,
            LoginBackgroundUrl = tenant.LoginBackgroundUrl,
            FooterText = tenant.FooterText
        };
    }
}
