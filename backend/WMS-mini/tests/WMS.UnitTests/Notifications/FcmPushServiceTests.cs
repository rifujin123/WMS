using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.UnitTests.Notifications;

public class FcmPushServiceTests
{
    private readonly Guid _tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _userId = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");

    private WmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);

        return new WmsDbContext(options, currentUserServiceMock.Object);
    }

    [Fact]
    public async Task RegisterTokenAsync_ShouldSaveToken_AndUpsertExisting()
    {
        // Arrange
        var context = CreateContext();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);

        var configMock = new Mock<IConfiguration>();
        var service = new FcmPushService(context, currentUserServiceMock.Object, configMock.Object, NullLogger<FcmPushService>.Instance);

        // Act 1: Register initial token
        var result1 = await service.RegisterTokenAsync(_userId, "token-123456", "Web-Chrome");
        var saved1 = await context.UserDeviceTokens.FirstOrDefaultAsync(t => t.UserId == _userId);

        // Assert 1
        Assert.True(result1);
        Assert.NotNull(saved1);
        Assert.Equal("token-123456", saved1.FcmToken);
        Assert.Equal("Web-Chrome", saved1.DeviceType);

        // Act 2: Re-register same token with updated device type (Upsert)
        var result2 = await service.RegisterTokenAsync(_userId, "token-123456", "Web-Edge");
        var totalTokens = await context.UserDeviceTokens.CountAsync(t => t.UserId == _userId);
        var updated = await context.UserDeviceTokens.FirstOrDefaultAsync(t => t.UserId == _userId);

        // Assert 2
        Assert.True(result2);
        Assert.Equal(1, totalTokens); // Không tạo token rác trùng lặp
        Assert.NotNull(updated);
        Assert.Equal("Web-Edge", updated.DeviceType);
    }

    [Fact]
    public async Task UnregisterTokenAsync_ShouldRemoveToken()
    {
        // Arrange
        var context = CreateContext();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);

        var configMock = new Mock<IConfiguration>();
        var service = new FcmPushService(context, currentUserServiceMock.Object, configMock.Object, NullLogger<FcmPushService>.Instance);

        await service.RegisterTokenAsync(_userId, "token-to-delete", "Web");

        // Act
        var result = await service.UnregisterTokenAsync(_userId, "token-to-delete");
        var remaining = await context.UserDeviceTokens.CountAsync(t => t.UserId == _userId);

        // Assert
        Assert.True(result);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task SendPushNotificationAsync_ShouldRunInOfflineSafeMode_WithoutThrowing()
    {
        // Arrange: Chạy hoàn toàn không có Firebase credentials (offline mock)
        var context = CreateContext();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);

        var configMock = new Mock<IConfiguration>();
        var service = new FcmPushService(context, currentUserServiceMock.Object, configMock.Object, NullLogger<FcmPushService>.Instance);

        await service.RegisterTokenAsync(_userId, "fcm-device-token-abc", "Web");

        // Act
        var pushCount = await service.SendPushNotificationAsync(
            new[] { _userId },
            "Đơn PO mới",
            "PO-001 đã được tạo",
            "/purchase-orders");

        // Assert: Trả về số lượng token được mô phỏng gửi thành công, không crash
        Assert.Equal(1, pushCount);
    }
}
