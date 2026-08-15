using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,WarehouseManager")]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AuditLogQueryDto query)
    {
        var result = await _service.GetAsync(query);
        return Ok(result);
    }

    [HttpGet("{entityType}/{entityId:guid}/status-history")]
    public async Task<IActionResult> GetStatusHistory(string entityType, Guid entityId)
    {
        var result = await _service.GetStatusHistoryAsync(entityType, entityId);
        return Ok(result);
    }
}
