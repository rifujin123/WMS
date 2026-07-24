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
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Create([FromBody] CreateReceivingDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result == null)
            return BadRequest(new { message = "PO not found or not in Approved status." });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPatch("{id}/confirm")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Confirm([FromRoute] Guid id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var result = await _service.ConfirmAsync(id);
        if (result == null) return BadRequest(new { message = "Receiving is not in Draft status." });
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        return Ok(new { message = "Deleted successfully" });
    }
}
