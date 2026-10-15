namespace WMS.Application.Interfaces;

public interface IFcmPushService
{
    Task<int> SendPushNotificationAsync(
        IEnumerable<Guid> userIds,
        string title,
        string message,
        string? actionUrl,
        CancellationToken cancellationToken = default);

    Task<bool> RegisterTokenAsync(
        Guid userId,
        string fcmToken,
        string deviceType,
        CancellationToken cancellationToken = default);

    Task<bool> UnregisterTokenAsync(
        Guid userId,
        string fcmToken,
        CancellationToken cancellationToken = default);
}
