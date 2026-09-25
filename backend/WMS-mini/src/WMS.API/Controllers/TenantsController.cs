using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
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
}
