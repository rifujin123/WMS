using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BackorderLinksController : ControllerBase
{
    private readonly IBackorderLinkService _service;

    public BackorderLinksController(IBackorderLinkService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? saleOrderId,
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] Guid? productId)
    {
        var result = await _service.GetAllAsync(saleOrderId, purchaseOrderId, productId);
        return Ok(result);
    }

    [HttpGet("{id}", Name = "GetBackorderLinkById")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Create([FromBody] CreateBackorderLinkDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtRoute("GetBackorderLinkById", new { id = result.Id }, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? Ok(new { message = "Deleted successfully" }) : NotFound();
    }
}
