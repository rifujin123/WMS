using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WMS.Application.Configuration;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.UnitTests.Notifications;

public class EmailAlertTests
{
    [Fact]
    public async Task SendAlertEmailAsync_WhenSimulated_LogsAndDoesNotThrow()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new EmailOptions
        {
            Provider = "Simulated",
            SmtpHost = "localhost",
            Username = ""
        });

        var loggerMock = new Mock<ILogger<EmailService>>();
        var emailService = new EmailService(optionsMock.Object, loggerMock.Object);

        // Act & Assert: Should execute safely without throwing
        await emailService.SendAlertEmailAsync("test@example.com", "Admin", "Test Subject", "<p>Alert Body</p>");
    }

    [Fact]
    public async Task TestEmailConnectionAsync_WhenSimulated_ReturnsTrue()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new EmailOptions
        {
            Provider = "Simulated"
        });

        var loggerMock = new Mock<ILogger<EmailService>>();
        var emailService = new EmailService(optionsMock.Object, loggerMock.Object);

        // Act
        var result = await emailService.TestEmailConnectionAsync("test@example.com");

        // Assert
        Assert.True(result);
    }
}
