using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WMS.API.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PutAwayTasksController : ControllerBase
{
    private readonly IPutAwayService _service;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly PaginationOptions _paginationOptions;

    public PutAwayTasksController(IPutAwayService service, UserManager<User> userManager, ICurrentUserService currentUser, IOptions<PaginationOptions> paginationOptions)
    {
        _service = service;
        _userManager = userManager;
        _currentUser = currentUser;
        _paginationOptions = paginationOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PutAwayTaskListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            return BadRequest(new { message = "Page must be greater than or equal to 1." });

        if (_currentUser.IsInRole("WarehouseStaff"))
            query = new PutAwayTaskListQuery { Page = query.Page, Status = query.Status, AssignToId = _currentUser.UserId };

        var result = await _service.GetPagedAsync(query, _paginationOptions.PageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] Guid? assignToId)
    {
        if (_currentUser.IsInRole("WarehouseStaff"))
            assignToId = _currentUser.UserId;

        return Ok(await _service.GetAllAsync(assignToId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Create([FromBody] CreatePutAwayTaskDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdatePutAwayTaskDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(new { message = "Deleted successfully" });
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Assign([FromRoute] Guid id, [FromBody] AssignPutAwayDto dto)
    {
        var target = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (target == null)
            return BadRequest(new { message = "User not found." });

        if (!await _userManager.IsInRoleAsync(target, "WarehouseStaff"))
            return BadRequest(new { message = "Can only assign to WarehouseStaff." });

        var result = await _service.AssignAsync(id, dto.UserId);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpPost("{id}/start")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> StartProgress([FromRoute] Guid id)
    {
        var result = await _service.StartProgressAsync(id);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin,WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Complete([FromRoute] Guid id)
    {
        var result = await _service.CompleteAsync(id);
        if (result == null)
            return NotFound(new { message = "PutAway task not found" });

        return Ok(result);
    }
}
