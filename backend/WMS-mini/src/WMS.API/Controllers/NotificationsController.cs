using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IFcmPushService _fcmPushService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        IFcmPushService fcmPushService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _fcmPushService = fcmPushService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications(
        [FromQuery] NotificationListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetNotificationsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<NotificationUnreadCountDto>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(new NotificationUnreadCountDto { Count = count });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var success = await _notificationService.MarkAsReadAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Không tìm thấy thông báo hoặc bạn không có quyền." });
        }

        return Ok(new { success = true, message = "Đã đánh dấu thông báo là đã đọc." });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        var updatedCount = await _notificationService.MarkAllAsReadAsync(cancellationToken);
        return Ok(new { updatedCount, message = $"Đã đánh dấu {updatedCount} thông báo là đã đọc." });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ChiefAccountant,WarehouseManager")]
    public async Task<ActionResult<NotificationDto>> CreateNotification(
        [FromBody] CreateNotificationDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.CreateNotificationAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("seed-sample")]
    public async Task<IActionResult> SeedSample(
        CancellationToken cancellationToken)
    {
        var seededCount = await _notificationService.SeedSampleNotificationsAsync(cancellationToken);
        return Ok(new { seededCount, message = $"Đã tạo {seededCount} thông báo mẫu thành công." });
    }
    [HttpPost("fcm-token")]
    public async Task<IActionResult> RegisterFcmToken(
        [FromBody] RegisterFcmTokenDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _fcmPushService.RegisterTokenAsync(currentUserId.Value, dto.FcmToken, dto.DeviceType ?? "Web", cancellationToken);
        return Ok(new { success, message = "Đã đăng ký thiết bị nhận thông báo thành công." });
    }

    [HttpDelete("fcm-token")]
    public async Task<IActionResult> UnregisterFcmToken(
        [FromBody] RegisterFcmTokenDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _fcmPushService.UnregisterTokenAsync(currentUserId.Value, dto.FcmToken, cancellationToken);
        return Ok(new { success, message = "Đã hủy đăng ký thiết bị." });
    }
}
