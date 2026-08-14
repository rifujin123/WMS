using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ReceivingsController : ControllerBase
{
    private readonly IReceivingService _service;

    public ReceivingsController(IReceivingService service)
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
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound(new { message = "Receiving not found" });

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Create([FromBody] CreateReceivingDto dto)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        var result = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Confirm([FromRoute] Guid id)
    {
        var result = await _service.ConfirmAsync(id);
        if (result == null)
            return NotFound(new { message = "Receiving not found" });

        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CreateReceivingDto dto)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        var result = await _service.UpdateAsync(id, dto, userId);
        if (result == null)
            return NotFound(new { message = "Receiving not found" });

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Receiving not found" });

        return Ok(new { message = "Deleted successfully" });
    }
}
