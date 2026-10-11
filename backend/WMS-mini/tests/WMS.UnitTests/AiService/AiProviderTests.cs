using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WMS.Application.Configuration;
using WMS.Domain.Entities;
using WMS.Infrastructure.Services;
using WMS.UnitTests.Common;
using Xunit;

namespace WMS.UnitTests.AiService;

public class AiProviderTests
{
    [Fact]
    public async Task ExtractInvoiceAsync_SuccessfulResponse_ParsesResultCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create(tenantId);
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Code = "TEST",
            AiApiKey = "test-tenant-key",
            AiModel = "gemini-1.5-flash",
        });
        await db.SaveChangesAsync();

        var jsonResponse = """
        {
            "invoice_number": "HD-2026-999",
            "vendor_name": "Cong ty TNHH Demo",
            "invoice_date": "2026-10-11T10:00:00",
            "line_items": [
                {
                    "sku": "SKU-A",
                    "name": "San pham A",
                    "quantity": 10,
                    "unit_price": 50000,
                    "total_amount": 500000
                }
            ],
            "confidence_score": 0.95
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:8000")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("AiProvider")).Returns(httpClient);

        var options = Options.Create(new AiProviderOptions
        {
            ServiceUrl = "http://localhost:8000",
            ApiKey = "fallback-key",
        });

        var provider = new AiProvider(factoryMock.Object, options, db);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image"));

        // Act
        var result = await provider.ExtractInvoiceAsync(stream, "invoice.png");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HD-2026-999", result.InvoiceNumber);
        Assert.Equal("Cong ty TNHH Demo", result.VendorName);
        Assert.Equal(0.95, result.ConfidenceScore);
        Assert.Single(result.LineItems);
        Assert.Equal("SKU-A", result.LineItems[0].Sku);
        Assert.Equal(10, result.LineItems[0].Quantity);
        Assert.Equal(50000m, result.LineItems[0].UnitPrice);
        Assert.Equal(500000m, result.LineItems[0].TotalAmount);
    }

    [Fact]
    public async Task ExtractInvoiceAsync_PythonServiceConnectionRefused_ThrowsHelpfulException()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:8000")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("AiProvider")).Returns(httpClient);

        var options = Options.Create(new AiProviderOptions
        {
            ServiceUrl = "http://localhost:8000",
            ApiKey = "fallback-key",
        });

        var provider = new AiProvider(factoryMock.Object, options, db);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.ExtractInvoiceAsync(stream, "invoice.png"));

        Assert.Contains("Không thể kết nối đến Dịch vụ AI (Python Service)", ex.Message);
    }

    [Fact]
    public async Task ExtractInvoiceAsync_NoApiKeyConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        using var db = TestDbContextFactory.Create();

        var factoryMock = new Mock<IHttpClientFactory>();
        var options = Options.Create(new AiProviderOptions
        {
            ServiceUrl = "http://localhost:8000",
            ApiKey = "", // No key
        });

        var provider = new AiProvider(factoryMock.Object, options, db);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ExtractInvoiceAsync(stream, "invoice.png"));

        Assert.Contains("chưa cấu hình Google Gemini API Key", ex.Message);
    }
}
