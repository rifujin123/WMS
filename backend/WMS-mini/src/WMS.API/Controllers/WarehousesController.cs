using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WMS.API.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _service;
    private readonly PaginationOptions _paginationOptions;

    public WarehousesController(IWarehouseService service, IOptions<PaginationOptions> paginationOptions)
    {
        _service = service;
        _paginationOptions = paginationOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] WarehouseListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            return BadRequest(new { message = "Page must be greater than or equal to 1." });

        var result = await _service.GetPagedAsync(query, _paginationOptions.PageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup()
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateWarehouseDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (result == DeleteWarehouseResult.NotFound)
            return NotFound();

        if (result == DeleteWarehouseResult.HasLocations)
            return Conflict(new { message = "Warehouse has locations and cannot be deleted." });

        return Ok(new {message = "Deleted successfully"});
    }
}
