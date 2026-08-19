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
public class StocksController : ControllerBase
{
    private readonly IStockService _service;
    private readonly PaginationOptions _paginationOptions;

    public StocksController(IStockService service, IOptions<PaginationOptions> paginationOptions)
    {
        _service = service;
        _paginationOptions = paginationOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? productId, [FromQuery] Guid? locationId)
    {
        if (productId.HasValue)
            return Ok(await _service.GetByProductAsync(productId.Value));

        if (locationId.HasValue)
            return Ok(await _service.GetByLocationAsync(locationId.Value));

        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] StockSummaryQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            return BadRequest(new { message = "Page must be greater than or equal to 1." });

        var result = await _service.GetSummaryPagedAsync(query, _paginationOptions.PageSize, cancellationToken);
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
}