using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly WmsDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFcmPushService? _fcmPushService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        WmsDbContext context,
        ICurrentUserService currentUserService,
        ILogger<NotificationService> logger,
        IFcmPushService? fcmPushService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _fcmPushService = fcmPushService;
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(NotificationListQuery query, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        var queryable = _context.Notifications.AsNoTracking();

        if (currentUserId.HasValue)
        {
            queryable = queryable.Where(n => n.UserId == currentUserId.Value);
        }

        if (query.UnreadOnly)
        {
            queryable = queryable.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            queryable = queryable.Where(n => n.Type == query.Type);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var items = await queryable
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                TenantId = n.TenantId,
                UserId = n.UserId,
                TargetRole = n.TargetRole,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return PagedResult<NotificationDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return 0;
        }

        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == currentUserId.Value && !n.IsRead)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return false;
        }

        // Đảm bảo chỉ chính chủ hoặc admin mới đánh dấu đọc được
        if (currentUserId.HasValue && notification.UserId.HasValue && notification.UserId != currentUserId.Value)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return 0;
        }

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId.Value && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadNotifications.Count == 0)
        {
            return 0;
        }

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unreadNotifications.Count;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId ?? WmsDbContext.DefaultTenantId;

        var entity = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = dto.UserId,
            TargetRole = dto.TargetRole,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            ActionUrl = dto.ActionUrl,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Notifications.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new NotificationDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            UserId = entity.UserId,
            TargetRole = entity.TargetRole,
            Title = entity.Title,
            Message = entity.Message,
            Type = entity.Type,
            ActionUrl = entity.ActionUrl,
            IsRead = entity.IsRead,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public async Task<int> SeedSampleNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return 0;
        }

        var tenantId = _currentUserService.TenantId ?? WmsDbContext.DefaultTenantId;

        var sampleItems = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = currentUserId.Value,
                TargetRole = "ChiefAccountant",
                Title = "PO-2026-003 cấn trừ công nợ",
                Message = "Đơn mua hàng PO-2026-003 có khoản cấn trừ công nợ 5,000,000 đ cần Kế toán trưởng phê duyệt.",
                Type = "ApprovalRequired",
                ActionUrl = "/purchase-orders",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = currentUserId.Value,
                TargetRole = "WarehouseStaff",
                Title = "Phân công cất hàng mới",
                Message = "Bạn được phân công nhiệm vụ cất hàng task #PA-002 tại kệ Storage LOC-HCM-A-01-02.",
                Type = "TaskAssigned",
                ActionUrl = "/putaway-tasks",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-25)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = currentUserId.Value,
                TargetRole = "WarehouseManager",
                Title = "Lô hàng cận hạn sử dụng (FEFO)",
                Message = "Lô hàng IP15-PRO tại vị trí LOC-HCM-A-01 còn dưới 15 ngày hết hạn. Ưu tiên xuất kho.",
                Type = "Reminder",
                ActionUrl = "/dashboard",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = currentUserId.Value,
                TargetRole = "Accountant",
                Title = "Đơn bán hàng đã nhận đủ cọc",
                Message = "Đơn bán SO-2026-005 đã nhận đủ 100% tiền cọc, chuyển sang trạng thái Chờ hàng (Backorder).",
                Type = "System",
                ActionUrl = "/sale-orders",
                IsRead = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.Notifications.AddRange(sampleItems);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} sample notifications for user {UserId}", sampleItems.Count, currentUserId.Value);
        return sampleItems.Count;
    }

    public async Task<int> PublishToRoleAsync(
        string role,
        string title,
        string message,
        string type,
        string? actionUrl,
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _currentUserService.TenantId ?? WmsDbContext.DefaultTenantId;
            var currentUserId = _currentUserService.UserId;

            // Query active users belonging to role and warehouse
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId);

            if (warehouseId.HasValue)
            {
                query = query.Where(u => !u.WarehouseId.HasValue || u.WarehouseId == warehouseId.Value);
            }

            var users = await query.ToListAsync(cancellationToken);
            if (users.Count == 0)
            {
                return 0;
            }

            // Lọc users theo Role thông qua AspNetUserRoles
            var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role, cancellationToken);
            if (roleEntity == null)
            {
                _logger.LogWarning("Role {Role} not found when publishing notifications", role);
                return 0;
            }

            var userIdsInRole = await _context.UserRoles
                .Where(ur => ur.RoleId == roleEntity.Id)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            var targetUsers = users
                .Where(u => userIdsInRole.Contains(u.Id))
                .Where(u => !currentUserId.HasValue || u.Id != currentUserId.Value) // Không gửi cho chính người tạo sự kiện
                .ToList();

            if (targetUsers.Count == 0)
            {
                return 0;
            }

            var notifications = targetUsers.Select(u => new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = u.Id,
                TargetRole = role,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken);

            if (_fcmPushService != null)
            {
                _ = _fcmPushService.SendPushNotificationAsync(targetUsers.Select(u => u.Id), title, message, actionUrl, cancellationToken);
            }

            _logger.LogInformation("Published {Count} notifications to role {Role}", notifications.Count, role);
            return notifications.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notifications to role {Role}", role);
            return 0;
        }
    }

    public async Task<NotificationDto?> PublishToUserAsync(
        Guid userId,
        string title,
        string message,
        string type,
        string? actionUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _currentUserService.TenantId ?? WmsDbContext.DefaultTenantId;
            var currentUserId = _currentUserService.UserId;

            // Nếu người nhận chính là người thực hiện, bỏ qua để tránh self-notification
            if (currentUserId.HasValue && currentUserId.Value == userId)
            {
                return null;
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when publishing notification", userId);
                return null;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            if (_fcmPushService != null)
            {
                _ = _fcmPushService.SendPushNotificationAsync(new[] { userId }, title, message, actionUrl, cancellationToken);
            }

            _logger.LogInformation("Published notification to user {UserId}: {Title}", userId, title);
            return new NotificationDto
            {
                Id = notification.Id,
                TenantId = notification.TenantId,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                ActionUrl = notification.ActionUrl,
                IsRead = notification.IsRead,
                CreatedAtUtc = notification.CreatedAtUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification to user {UserId}", userId);
            return null;
        }
    }
}
