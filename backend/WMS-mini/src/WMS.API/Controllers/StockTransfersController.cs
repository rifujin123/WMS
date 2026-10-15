using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StockTransfersController : ControllerBase
{
    private readonly IStockTransferService _service;

    public StockTransfersController(IStockTransferService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,ChiefAccountant,Accountant,WarehouseManager")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,ChiefAccountant,Accountant,WarehouseManager")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Phiếu điều chuyển không tồn tại." });
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Create([FromBody] CreateStockTransferDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/ship")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Ship([FromRoute] Guid id)
    {
        try
        {
            var result = await _service.ShipAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/receive")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Receive([FromRoute] Guid id, [FromBody] ReceiveStockTransferDto dto)
    {
        try
        {
            var result = await _service.ReceiveAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/cancel")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        try
        {
            var success = await _service.CancelAsync(id);
            if (!success) return NotFound(new { message = "Không tìm thấy phiếu điều chuyển." });
            return Ok(new { message = "Hủy phiếu điều chuyển thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
