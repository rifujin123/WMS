using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Services;
using WMS.UnitTests.Common;
using Xunit;

namespace WMS.UnitTests.IdentityAndOnboarding;

public class TenantBrandingAndAiKeyTests
{
    private static Mock<UserManager<User>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetCurrentTenantConfigAsync_Returns_MaskedAiKey_And_Branding()
    {
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Logistics Alpha",
                Code = "alpha",
                LogoUrl = "https://example.com/logo.png",
                LoginBackgroundUrl = "https://example.com/bg.jpg",
                FooterText = "Hotline 1900",
                AiApiKey = "AIzaSyD1234567890SecretKey",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(tenantId, dbName))
        {
            var mockUserManager = CreateMockUserManager();
            var mockEmailService = new Mock<IEmailService>();
            var mockLogger = new Mock<ILogger<TenantService>>();
            var mockHttpFactory = new Mock<IHttpClientFactory>();

            var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockHttpFactory.Object, mockLogger.Object);

            var config = await service.GetCurrentTenantConfigAsync();

            Assert.NotNull(config);
            Assert.Equal("Logistics Alpha", config.CompanyName);
            Assert.Equal("alpha", config.CompanyCode);
            Assert.Equal("https://example.com/logo.png", config.LogoUrl);
            Assert.Equal("https://example.com/bg.jpg", config.LoginBackgroundUrl);
            Assert.Equal("Hotline 1900", config.FooterText);
            Assert.True(config.HasAiKeyConfigured);
            Assert.NotNull(config.MaskedAiApiKey);
            Assert.StartsWith("AIzaSy...****", config.MaskedAiApiKey);
            Assert.DoesNotContain("SecretKey", config.MaskedAiApiKey);
        }
    }

    [Fact]
    public async Task UpdateTenantConfigAsync_Updates_Branding_And_Preserves_AiKey_When_Masked()
    {
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Alpha Old Name",
                Code = "alpha",
                AiApiKey = "OriginalSecretKey12345",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(tenantId, dbName))
        {
            var mockUserManager = CreateMockUserManager();
            var mockEmailService = new Mock<IEmailService>();
            var mockLogger = new Mock<ILogger<TenantService>>();
            var mockHttpFactory = new Mock<IHttpClientFactory>();

            var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockHttpFactory.Object, mockLogger.Object);

            var updateDto = new UpdateTenantConfigDto
            {
                CompanyName = "Alpha Logistics Global",
                LogoUrl = "https://example.com/new-logo.png",
                LoginBackgroundUrl = "https://example.com/new-bg.jpg",
                FooterText = "Custom Footer 2026",
                AiApiKey = "Origin...****" // Masked input should NOT overwrite original secret
            };

            await service.UpdateTenantConfigAsync(updateDto);
        }

        using (var dbAssert = TestDbContextFactory.Create(tenantId, dbName))
        {
            var tenant = await dbAssert.Tenants.FindAsync(tenantId);
            Assert.NotNull(tenant);
            Assert.Equal("Alpha Logistics Global", tenant.Name);
            Assert.Equal("https://example.com/new-logo.png", tenant.LogoUrl);
            Assert.Equal("https://example.com/new-bg.jpg", tenant.LoginBackgroundUrl);
            Assert.Equal("Custom Footer 2026", tenant.FooterText);
            Assert.Equal("OriginalSecretKey12345", tenant.AiApiKey); // Preserved
        }
    }

    [Fact]
    public async Task UpdateTenantConfigAsync_Overwrites_AiKey_When_New_Key_Provided()
    {
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Alpha",
                Code = "alpha",
                AiApiKey = "OldKey",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(tenantId, dbName))
        {
            var mockUserManager = CreateMockUserManager();
            var mockEmailService = new Mock<IEmailService>();
            var mockLogger = new Mock<ILogger<TenantService>>();
            var mockHttpFactory = new Mock<IHttpClientFactory>();

            var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockHttpFactory.Object, mockLogger.Object);

            await service.UpdateTenantConfigAsync(new UpdateTenantConfigDto
            {
                CompanyName = "Alpha",
                AiApiKey = "AIzaSyBrandNewKey999"
            });
        }

        using (var dbAssert = TestDbContextFactory.Create(tenantId, dbName))
        {
            var tenant = await dbAssert.Tenants.FindAsync(tenantId);
            Assert.NotNull(tenant);
            Assert.Equal("AIzaSyBrandNewKey999", tenant.AiApiKey);
        }
    }

    [Fact]
    public async Task TestAiKeyAsync_Success_Returns_True()
    {
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Alpha",
                Code = "alpha",
                AiApiKey = "ValidKey",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(tenantId, dbName))
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"candidates\":[]}")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var mockHttpFactory = new Mock<IHttpClientFactory>();
            mockHttpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var mockUserManager = CreateMockUserManager();
            var mockEmailService = new Mock<IEmailService>();
            var mockLogger = new Mock<ILogger<TenantService>>();

            var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockHttpFactory.Object, mockLogger.Object);

            var result = await service.TestAiKeyAsync(new TestAiKeyDto { ApiKey = "ValidKey" });

            Assert.True(result.Success);
            Assert.Contains("thành công", result.Message);
        }
    }

    [Fact]
    public async Task GetPublicBrandingAsync_Returns_Branding_For_Valid_TenantCode()
    {
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        using (var db = TestDbContextFactory.Create(null, dbName))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Beta Warehouses",
                Code = "beta-corp",
                LogoUrl = "https://example.com/beta-logo.png",
                LoginBackgroundUrl = "https://example.com/beta-bg.jpg",
                FooterText = "Beta Hotline 1800",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var dbTest = TestDbContextFactory.Create(null, dbName))
        {
            var mockUserManager = CreateMockUserManager();
            var mockEmailService = new Mock<IEmailService>();
            var mockLogger = new Mock<ILogger<TenantService>>();
            var mockHttpFactory = new Mock<IHttpClientFactory>();

            var service = new TenantService(dbTest, mockUserManager.Object, mockEmailService.Object, mockHttpFactory.Object, mockLogger.Object);

            var branding = await service.GetPublicBrandingAsync("beta-corp");

            Assert.NotNull(branding);
            Assert.Equal("Beta Warehouses", branding.CompanyName);
            Assert.Equal("https://example.com/beta-logo.png", branding.LogoUrl);
            Assert.Equal("https://example.com/beta-bg.jpg", branding.LoginBackgroundUrl);
            Assert.Equal("Beta Hotline 1800", branding.FooterText);
        }
    }
}