using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StockAdjustmentsController : ControllerBase
{
    private readonly IStockAdjustmentService _service;

    public StockAdjustmentsController(IStockAdjustmentService service)
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
            return NotFound(new { message = "StockAdjustment not found" });

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Create([FromBody] CreateStockAdjustmentDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Approve([FromRoute] Guid id)
    {
        var result = await _service.ApproveAsync(id);
        if (result == null)
            return NotFound(new { message = "StockAdjustment not found" });

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "StockAdjustment not found" });

        return Ok(new { message = "Deleted successfully" });
    }
}
