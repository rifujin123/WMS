using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.UnitTests.Notifications;

public class NotificationServiceTests
{
    private readonly Guid _tenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _tenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _user1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _user2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private WmsDbContext CreateInMemoryDbContext(Guid? currentTenantId, Guid? currentUserId)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(currentTenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(currentUserId);

        return new WmsDbContext(options, currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCount_OnlyForCurrentUserIdAndUnread()
    {
        // Arrange
        var context = CreateInMemoryDbContext(_tenantA, _user1);
        var serviceMock = new Mock<ICurrentUserService>();
        serviceMock.Setup(s => s.TenantId).Returns(_tenantA);
        serviceMock.Setup(s => s.UserId).Returns(_user1);

        var service = new NotificationService(context, serviceMock.Object, NullLogger<NotificationService>.Instance);

        context.Notifications.AddRange(
            new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user1, Title = "N1", Message = "M1", IsRead = false },
            new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user1, Title = "N2", Message = "M2", IsRead = false },
            new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user1, Title = "N3", Message = "M3", IsRead = true },
            new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user2, Title = "N4", Message = "M4", IsRead = false }
        );
        await context.SaveChangesAsync();

        // Act
        var count = await service.GetUnreadCountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkTrue_WhenOwnerRequests()
    {
        // Arrange
        var context = CreateInMemoryDbContext(_tenantA, _user1);
        var notifId = Guid.NewGuid();

        var serviceMock = new Mock<ICurrentUserService>();
        serviceMock.Setup(s => s.TenantId).Returns(_tenantA);
        serviceMock.Setup(s => s.UserId).Returns(_user1);

        var service = new NotificationService(context, serviceMock.Object, NullLogger<NotificationService>.Instance);

        context.Notifications.Add(new Notification
        {
            Id = notifId,
            TenantId = _tenantA,
            UserId = _user1,
            Title = "PO Pending",
            Message = "Need approval",
            IsRead = false
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.MarkAsReadAsync(notifId);
        var notifInDb = await context.Notifications.FindAsync(notifId);

        // Assert
        Assert.True(result);
        Assert.NotNull(notifInDb);
        Assert.True(notifInDb.IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkOnlyCurrentUserNotifications()
    {
        // Arrange
        var context = CreateInMemoryDbContext(_tenantA, _user1);

        var serviceMock = new Mock<ICurrentUserService>();
        serviceMock.Setup(s => s.TenantId).Returns(_tenantA);
        serviceMock.Setup(s => s.UserId).Returns(_user1);

        var service = new NotificationService(context, serviceMock.Object, NullLogger<NotificationService>.Instance);

        var notif1 = new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user1, Title = "U1-A", Message = "A", IsRead = false };
        var notif2 = new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user1, Title = "U1-B", Message = "B", IsRead = false };
        var notifOther = new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user2, Title = "U2-A", Message = "C", IsRead = false };

        context.Notifications.AddRange(notif1, notif2, notifOther);
        await context.SaveChangesAsync();

        // Act
        var updatedCount = await service.MarkAllAsReadAsync();

        // Assert
        Assert.Equal(2, updatedCount);
        var unreadUser1 = await context.Notifications.CountAsync(n => n.UserId == _user1 && !n.IsRead);
        var unreadUser2 = await context.Notifications.CountAsync(n => n.UserId == _user2 && !n.IsRead);

        Assert.Equal(0, unreadUser1);
        Assert.Equal(1, unreadUser2); // User 2 không bị ảnh hưởng
    }

    [Fact]
    public async Task TenantIsolation_ShouldNotLeakNotificationsAcrossTenants()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // User thuộc Tenant A
        var userAServiceMock = new Mock<ICurrentUserService>();
        userAServiceMock.Setup(s => s.TenantId).Returns(_tenantA);
        userAServiceMock.Setup(s => s.UserId).Returns(_user1);

        using (var seedContext = new WmsDbContext(options))
        {
            seedContext.Notifications.AddRange(
                new Notification { Id = Guid.NewGuid(), TenantId = _tenantA, UserId = _user1, Title = "Tenant A Notif", Message = "Msg A", IsRead = false },
                new Notification { Id = Guid.NewGuid(), TenantId = _tenantB, UserId = _user1, Title = "Tenant B Notif", Message = "Msg B", IsRead = false }
            );
            await seedContext.SaveChangesAsync();
        }

        using (var tenantAContext = new WmsDbContext(options, userAServiceMock.Object))
        {
            var service = new NotificationService(tenantAContext, userAServiceMock.Object, NullLogger<NotificationService>.Instance);

            // Act
            var result = await service.GetNotificationsAsync(new NotificationListQuery());

            // Assert: Chỉ thấy thông báo của Tenant A do Global Query Filter
            Assert.Single(result.Items);
            Assert.Equal("Tenant A Notif", result.Items[0].Title);
        }
    }
}
