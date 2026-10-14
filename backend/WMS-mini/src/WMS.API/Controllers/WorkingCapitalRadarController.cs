using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,ChiefAccountant,WarehouseManager,Accountant")]
[Route("api/[controller]")]
[Route("api/working-capital-radar")]
public class WorkingCapitalRadarController : ControllerBase
{
    private readonly IWorkingCapitalRadarService _radarService;

    public WorkingCapitalRadarController(IWorkingCapitalRadarService radarService)
    {
        _radarService = radarService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? warehouseId, CancellationToken cancellationToken)
    {
        var data = await _radarService.GetRadarDataAsync(warehouseId, cancellationToken);
        return Ok(data);
    }
}
