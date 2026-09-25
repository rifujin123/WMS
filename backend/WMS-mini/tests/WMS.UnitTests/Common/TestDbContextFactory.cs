using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Data;

namespace WMS.UnitTests.Common;

public static class TestDbContextFactory
{
    public static WmsDbContext Create(Guid? currentTenantId = null, string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.TenantId).Returns(currentTenantId);
        mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        mockCurrentUserService.Setup(s => s.UserName).Returns("test_user");

        return new WmsDbContext(options, mockCurrentUserService.Object);
    }
}
