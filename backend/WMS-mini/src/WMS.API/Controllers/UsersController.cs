using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();

        var profile = await _userService.GetProfileAsync(userId);
        if (profile == null) return NotFound(new { message = "User not found." });
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();

        try
        {
            var profile = await _userService.UpdateProfileAsync(userId, dto);
            if (profile == null) return NotFound(new { message = "User not found." });
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();

        try
        {
            await _userService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();

        // Validate lại ở server, không tin FE
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file ảnh." });
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { message = "Ảnh phải nhỏ hơn 2MB." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Chỉ nhận ảnh JPG, PNG hoặc WEBP." });

        try
        {
            await using var stream = file.OpenReadStream();
            var avatarUrl = await _userService.UploadAvatarAsync(userId, stream, file.FileName);
            if (avatarUrl == null) return NotFound(new { message = "User not found." });
            return Ok(new { avatarUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}