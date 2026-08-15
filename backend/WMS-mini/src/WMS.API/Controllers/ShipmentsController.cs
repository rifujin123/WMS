using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _service;

    public ShipmentsController(IShipmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? saleOrderId)
    {
        if (saleOrderId.HasValue)
        {
            var shipment = await _service.GetBySaleOrderAsync(saleOrderId.Value);
            if(shipment == null)
                return NotFound();
            return Ok(shipment);
        }

        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if(result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Create([FromBody] CreateShipmentDto dto)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        var result = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
