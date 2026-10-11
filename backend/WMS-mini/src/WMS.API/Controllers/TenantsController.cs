using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IImageService _imageService;

    public TenantsController(ITenantService tenantService, IImageService imageService)
    {
        _tenantService = tenantService;
        _imageService = imageService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTenantDto dto)
    {
        try
        {
            var origin = Request.Headers["Origin"].FirstOrDefault()
                ?? Request.Headers["Referer"].FirstOrDefault();

            await _tenantService.RegisterTenantAsync(dto, origin);
            return Accepted(new
            {
                message = "Đăng ký doanh nghiệp thành công. Vui lòng kiểm tra email để kích hoạt tài khoản."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã có lỗi xảy ra trong quá trình xử lý đăng ký.", details = ex.Message });
        }
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        try
        {
            await _tenantService.VerifyEmailAsync(dto);
            return Ok(new
            {
                message = "Xác thực email thành công! Tài khoản của bạn đã được kích hoạt."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã có lỗi xảy ra trong quá trình xác thực email.", details = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        try
        {
            var config = await _tenantService.GetCurrentTenantConfigAsync();
            return Ok(config);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Không thể lấy thông tin cấu hình.", details = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateTenantConfigDto dto)
    {
        try
        {
            await _tenantService.UpdateTenantConfigAsync(dto);
            return Ok(new { message = "Cập nhật cấu hình doanh nghiệp thành công!" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Không thể cập nhật cấu hình.", details = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("config/test-ai-key")]
    public async Task<IActionResult> TestAiKey([FromBody] TestAiKeyDto dto)
    {
        var result = await _tenantService.TestAiKeyAsync(dto);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("public-branding")]
    public async Task<IActionResult> GetPublicBranding([FromQuery] string? tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant))
        {
            return Ok(new PublicTenantBrandingDto());
        }

        var branding = await _tenantService.GetPublicBrandingAsync(tenant);
        if (branding == null)
        {
            return NotFound(new { message = "Không tìm thấy thông tin thương hiệu của doanh nghiệp này." });
        }

        return Ok(branding);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("upload-asset")]
    public async Task<IActionResult> UploadAsset([FromForm] IFormFile file, [FromQuery] string type)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn tệp ảnh hợp lệ." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Dung lượng ảnh không được vượt quá 5MB." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "Chỉ chấp nhận tệp ảnh JPG, PNG hoặc WEBP." });

        try
        {
            var isLogo = string.Equals(type, "logo", StringComparison.OrdinalIgnoreCase);
            var publicId = $"tenant_{Guid.NewGuid():N}";
            var width = isLogo ? 400 : 1920;
            var height = isLogo ? 400 : 1080;

            using var stream = file.OpenReadStream();
            var url = await _imageService.UploadAsync(stream, file.FileName, publicId, width, height);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Tải ảnh lên thất bại.", details = ex.Message });
        }
    }
}
