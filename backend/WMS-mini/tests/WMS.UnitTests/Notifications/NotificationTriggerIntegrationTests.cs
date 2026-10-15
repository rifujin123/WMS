using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.UnitTests.Notifications;

public class NotificationTriggerIntegrationTests
{
    private readonly Guid _tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _chiefAccountantId = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
    private readonly Guid _warehouseManagerId = Guid.Parse("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
    private readonly Guid _staffId = Guid.Parse("cccccccc-3333-3333-3333-cccccccccccc");
    private readonly Guid _warehouseId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private WmsDbContext CreateContext(Guid? currentUserId)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(currentUserId);

        return new WmsDbContext(options, currentUserServiceMock.Object);
    }

    [Fact]
    public async Task PublishToRoleAsync_ShouldCreateNotification_ForUsersInRoleExcludingActor()
    {
        // Arrange
        var context = CreateContext(_warehouseManagerId);
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(_warehouseManagerId);

        var service = new NotificationService(context, currentUserServiceMock.Object, NullLogger<NotificationService>.Instance);

        // Seed Roles and Users
        var role = new Microsoft.AspNetCore.Identity.IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "ChiefAccountant", NormalizedName = "CHIEFACCOUNTANT" };
        var user1 = new User { Id = _chiefAccountantId, UserName = "chief1", TenantId = _tenantId };
        var user2 = new User { Id = Guid.NewGuid(), UserName = "chief2", TenantId = _tenantId };

        context.Roles.Add(role);
        context.Users.AddRange(user1, user2);
        context.UserRoles.AddRange(
            new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { RoleId = role.Id, UserId = user1.Id },
            new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { RoleId = role.Id, UserId = user2.Id }
        );
        await context.SaveChangesAsync();

        // Act
        var publishedCount = await service.PublishToRoleAsync(
            "ChiefAccountant",
            "PO cần duyệt cấn trừ nợ",
            "PO-001 có cấn trừ nợ 5tr.",
            "ApprovalRequired",
            "/purchase-orders");

        // Assert
        Assert.Equal(2, publishedCount);
        var notifications = await context.Notifications.ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, n =>
        {
            Assert.Equal("ApprovalRequired", n.Type);
            Assert.False(n.IsRead);
            Assert.Equal("/purchase-orders", n.ActionUrl);
        });
    }

    [Fact]
    public async Task PublishToUserAsync_ShouldCreateNotification_ForTargetUser()
    {
        // Arrange
        var context = CreateContext(_warehouseManagerId);
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(_warehouseManagerId);

        var service = new NotificationService(context, currentUserServiceMock.Object, NullLogger<NotificationService>.Instance);

        var user = new User { Id = _staffId, UserName = "staff1", TenantId = _tenantId };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PublishToUserAsync(
            _staffId,
            "Phân công cất hàng mới",
            "Bạn được phân công task cất hàng PA-01",
            "TaskAssigned",
            "/putaway-tasks");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_staffId, result.UserId);
        Assert.Equal("TaskAssigned", result.Type);

        var notifInDb = await context.Notifications.FirstOrDefaultAsync(n => n.UserId == _staffId);
        Assert.NotNull(notifInDb);
        Assert.Equal("Phân công cất hàng mới", notifInDb.Title);
    }

    [Fact]
    public async Task PublishToUserAsync_ShouldPreventSelfNotification_WhenActorIsTarget()
    {
        // Arrange
        var context = CreateContext(_staffId);
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(_staffId);

        var service = new NotificationService(context, currentUserServiceMock.Object, NullLogger<NotificationService>.Instance);

        var user = new User { Id = _staffId, UserName = "staff1", TenantId = _tenantId };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PublishToUserAsync(
            _staffId,
            "Tự mình làm",
            "Không cần bắn cho chính mình",
            "System",
            "/");

        // Assert: Không sinh thông báo cho chính mình
        Assert.Null(result);
        var notifCount = await context.Notifications.CountAsync();
        Assert.Equal(0, notifCount);
    }
}
