using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Route("api/vendor-debts")]
public class VendorDebtsController : ControllerBase
{
    private readonly IVendorDebtService _service;

    public VendorDebtsController(IVendorDebtService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? vendorId,
        [FromQuery] VendorDebtType? debtType,
        [FromQuery] bool? isResolved)
    {
        var result = await _service.GetAllAsync(vendorId, debtType, isResolved);
        return Ok(result);
    }

    [HttpGet("unresolved/{vendorId}")]
    public async Task<IActionResult> GetUnresolvedByVendor([FromRoute] Guid vendorId)
    {
        var result = await _service.GetUnresolvedByVendorIdAsync(vendorId);
        return Ok(result);
    }
}
