using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,WarehouseManager")]
[Route("api/status-histories")]
public class StatusHistoriesController : ControllerBase
{
    private readonly IAuditLogService _service;

    public StatusHistoriesController(IAuditLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] StatusHistoryQueryDto query)
    {
        var result = await _service.GetStatusHistoriesAsync(query);
        return Ok(result);
    }
}
