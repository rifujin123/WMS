using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WMS.API.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;
    private readonly PaginationOptions _paginationOptions;

    public PurchaseOrdersController(IPurchaseOrderService service, IOptions<PaginationOptions> paginationOptions)
    {
        _service = service;
        _paginationOptions = paginationOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseOrderListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            return BadRequest(new { message = "Page must be greater than or equal to 1." });

        var result = await _service.GetPagedAsync(query, _paginationOptions.PageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdatePurchaseOrderDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result == null) return NotFound();
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

    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve([FromRoute] Guid id)
    {
        var result = await _service.ApproveAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPatch("{id}/close")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Close([FromRoute] Guid id)
    {
        var result = await _service.CloseAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
