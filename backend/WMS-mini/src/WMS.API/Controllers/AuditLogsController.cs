using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WMS.API.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,WarehouseManager")]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;
    private readonly PaginationOptions _paginationOptions;

    public AuditLogsController(IAuditLogService service, IOptions<PaginationOptions> paginationOptions)
    {
        _service = service;
        _paginationOptions = paginationOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AuditLogQueryDto query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            return BadRequest(new { message = "Page must be greater than or equal to 1." });

        var result = await _service.GetAsync(query, _paginationOptions.PageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("status-history")]
    public async Task<IActionResult> GetStatusHistory([FromQuery] string entityType, [FromQuery] Guid entityId)
    {
        var result = await _service.GetStatusHistoryAsync(entityType, entityId);
        return Ok(result);
    }
}
