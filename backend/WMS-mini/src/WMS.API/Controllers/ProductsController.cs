using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

using System.Security.Claims;
namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound(new { message = "Product not found" });

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromForm] CreateProductDto dto, [FromForm] IFormFile? file)
    {
        if(!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn ảnh sản phẩm." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Ảnh phải nhỏ hơn 5MB." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _service.CreateAsync(dto, userId, stream, file.FileName);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateProductDto dto, [FromForm] IFormFile? file)
    {
        if (file != null && file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Ảnh phải nhỏ hơn 5MB." });

        if (file != null)
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType))
                return BadRequest(new { message = "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF." });
        }

        try
        {
            await using var stream = file?.OpenReadStream();
            var result = await _service.UpdateAsync(id, dto, stream, file?.FileName);
            if (result == null)
                return NotFound(new { message = "Product not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return Ok(new { message = "Deleted successfully" });
    }

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImage(Guid id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file ảnh." });
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Ảnh phải nhỏ hơn 5MB." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF." });

        try
        {
            await using var stream = file.OpenReadStream();
            var imageUrl = await _service.UploadImageAsync(id, stream, file.FileName);
            if (imageUrl == null) return NotFound(new { message = "Product not found" });
            return Ok(new { imageUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
