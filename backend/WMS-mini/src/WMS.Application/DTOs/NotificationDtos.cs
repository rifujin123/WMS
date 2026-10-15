namespace WMS.Application.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? TargetRole { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "System"; // TaskAssigned | ApprovalRequired | Reminder | System
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class NotificationListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool UnreadOnly { get; set; } = false;
    public string? Type { get; set; }
}

public class NotificationUnreadCountDto
{
    public int Count { get; set; }
}

public class CreateNotificationDto
{
    public Guid? UserId { get; set; }
    public string? TargetRole { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "System";
    public string? ActionUrl { get; set; }
}
