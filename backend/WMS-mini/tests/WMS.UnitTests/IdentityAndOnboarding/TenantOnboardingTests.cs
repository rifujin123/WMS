using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Services;
using WMS.UnitTests.Common;

namespace WMS.UnitTests.IdentityAndOnboarding;

public class TenantOnboardingTests
{
    private static Mock<UserManager<User>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task RegisterTenant_ShouldThrowException_WhenCompanyCodeAlreadyExists()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var existingTenantId = Guid.NewGuid();

        using (var db = TestDbContextFactory.Create(existingTenantId, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = existingTenantId,
                Name = "Công ty Cũ",
                Code = "dup-code",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(null, dbName))
        {
            var mockUserManager = CreateMockUserManager();
            var mockEmailService = new Mock<IEmailService>();
            var mockLogger = new Mock<ILogger<TenantService>>();

            var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockLogger.Object);

            var dto = new RegisterTenantDto
            {
                CompanyName = "Công ty Mới",
                CompanyCode = "dup-code", // Trùng mã
                AdminFullName = "Admin Mới",
                AdminEmail = "newadmin@example.com",
                Password = "Password@123"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterTenantAsync(dto));
            Assert.Contains("Mã doanh nghiệp đã tồn tại", ex.Message);
        }
    }

    [Fact]
    public async Task RegisterTenant_ShouldThrowException_WhenEmailAlreadyInUse()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var dbTest = TestDbContextFactory.Create(null, dbName);

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(new User { Email = "existing@example.com" });

        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<TenantService>>();

        var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockLogger.Object);

        var dto = new RegisterTenantDto
        {
            CompanyName = "Công ty Mới",
            CompanyCode = "unique-code",
            AdminFullName = "Admin Mới",
            AdminEmail = "existing@example.com", // Trùng email
            Password = "Password@123"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterTenantAsync(dto));
        Assert.Contains("Email này đã được sử dụng", ex.Message);
    }

    [Fact]
    public async Task VerifyEmail_ShouldActivateTenant_WhenTokenIsValid()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var adminEmail = "verify-me@example.com";
        var user = new User { Id = Guid.NewGuid(), Email = adminEmail, TenantId = tenantId };

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Công ty Chờ Kích Hoạt",
                Code = "pending-tenant",
                IsActive = false
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(null, dbName))
        {
            var mockUserManager = CreateMockUserManager();
            mockUserManager.Setup(m => m.FindByEmailAsync(adminEmail)).ReturnsAsync(user);
            mockUserManager.Setup(m => m.ConfirmEmailAsync(user, "valid-token"))
                .ReturnsAsync(IdentityResult.Success);

            var mockEmailService = new Mock<IEmailService>();
            var mockLogger = new Mock<ILogger<TenantService>>();

            var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockLogger.Object);

            // Act
            await service.VerifyEmailAsync(new VerifyEmailDto { Email = adminEmail, Token = "valid-token" });

            // Assert
            var activatedTenant = await dbTest.Tenants.FindAsync(tenantId);
            Assert.NotNull(activatedTenant);
            Assert.True(activatedTenant.IsActive);
            Assert.NotNull(activatedTenant.VerifiedDate);
        }
    }

    [Fact]
    public async Task Login_ShouldThrowUnauthorized_WhenTenantIsInactive()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var email = "admin@inactive.com";
        var user = new User { Id = Guid.NewGuid(), Email = email, UserName = email, TenantId = tenantId };

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Công ty Chưa Active",
                Code = "inactive-co",
                IsActive = false
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(tenantId, dbName))
        {
            var mockUserManager = CreateMockUserManager();
            mockUserManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);
            mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password@123")).ReturnsAsync(true);

            var mockConfig = new Mock<IConfiguration>();
            var mockCurrentUserService = new Mock<ICurrentUserService>();

            var authService = new AuthService(mockUserManager.Object, mockConfig.Object, dbTest, mockCurrentUserService.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                authService.LoginAsync(new LoginDto { Username = email, Password = "Password@123" }));

            Assert.Contains("chưa được kích hoạt", ex.Message);
        }
    }
}
