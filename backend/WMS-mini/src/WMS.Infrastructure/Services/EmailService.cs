using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string toEmail, string toName, string verificationLink)
    {
        _logger.LogInformation(
            "\n================================================================================\n" +
            "[SIMULATED EMAIL DISPATCH]\n" +
            "To: {ToName} <{ToEmail}>\n" +
            "Subject: Kích hoạt tài khoản Doanh nghiệp WMS\n" +
            "Body:\n" +
            "Xin chào {ToName},\n" +
            "Cảm ơn bạn đã đăng ký tài khoản doanh nghiệp trên nền tảng WMS.\n" +
            "Vui lòng nhấp vào liên kết sau để kích hoạt không gian kho của bạn:\n" +
            "{VerificationLink}\n" +
            "================================================================================\n",
            toName, toEmail, toName, verificationLink);

        return Task.CompletedTask;
    }
}
