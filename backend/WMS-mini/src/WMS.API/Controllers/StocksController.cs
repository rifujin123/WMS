using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockService _service;

    public StocksController(IStockService service)
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
            return NotFound(new { message = "Stock not found" });

        return Ok(result);
    }

    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetByProduct([FromRoute] Guid productId)
    {
        var result = await _service.GetByProductAsync(productId);
        return Ok(result);
    }
}