using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(NotificationListQuery query, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);
    Task<int> SeedSampleNotificationsAsync(CancellationToken cancellationToken = default);

    // Phase 2: Event Triggers & Role Broadcast Helpers
    Task<int> PublishToRoleAsync(string role, string title, string message, string type, string? actionUrl, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<NotificationDto?> PublishToUserAsync(Guid userId, string title, string message, string type, string? actionUrl, CancellationToken cancellationToken = default);
}
