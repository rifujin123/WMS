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
public class ReceivingsController : ControllerBase
{
    private readonly IReceivingService _service;
    private readonly IInvoiceScanService _scanService;
    private readonly PaginationOptions _paginationOptions;

    public ReceivingsController(IReceivingService service, IInvoiceScanService scanService, IOptions<PaginationOptions> paginationOptions)
    {
        _service = service;
        _scanService = scanService;
        _paginationOptions = paginationOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ReceivingListQuery query, CancellationToken cancellationToken)
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

    [HttpPost("scan")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Scan([FromForm] Guid purchaseOrderId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn ảnh hóa đơn." });
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Ảnh phải nhỏ hơn 5MB." });

        var allowed = new[] { "image/jpeg", "image/png" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Chỉ nhận ảnh JPG hoặc PNG." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _scanService.ScanAsync(purchaseOrderId, stream, file.FileName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound(new { message = "Receiving not found" });

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Create([FromBody] CreateReceivingDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Confirm([FromRoute] Guid id)
    {
        var result = await _service.ConfirmAsync(id);
        if (result == null)
            return NotFound(new { message = "Receiving not found" });

        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CreateReceivingDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result == null)
            return NotFound(new { message = "Receiving not found" });

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Receiving not found" });

        return Ok(new { message = "Deleted successfully" });
    }
}
