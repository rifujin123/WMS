using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PutAwayTasksController : ControllerBase
{
    private readonly IPutAwayService _service;

    public PutAwayTasksController(IPutAwayService service)
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
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Create([FromBody] CreatePutAwayTaskDto dto)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        var result = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdatePutAwayTaskDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(new { message = "Deleted successfully" });
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Assign([FromRoute] Guid id, [FromBody] AssignPutAwayDto dto)
    {
        var result = await _service.AssignAsync(id, dto.UserId);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpPost("{id}/start")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> StartProgress([FromRoute] Guid id)
    {
        var result = await _service.StartProgressAsync(id);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Complete([FromRoute] Guid id)
    {
        var result = await _service.CompleteAsync(id);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }
}