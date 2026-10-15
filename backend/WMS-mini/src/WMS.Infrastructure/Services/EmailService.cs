using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options;
        _logger = logger;
    }

    private bool IsSimulated(EmailOptions config)
    {
        return string.Equals(config.Provider, "Simulated", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(config.SmtpHost)
            || string.IsNullOrWhiteSpace(config.Username);
    }

    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verificationLink)
    {
        var emailConfig = _options.Value;

        if (IsSimulated(emailConfig))
        {
            _logger.LogInformation(
                "\n================================================================================\n" +
                "[SIMULATED EMAIL DISPATCH - DEV ENVIRONMENT]\n" +
                "To: {ToName} <{ToEmail}>\n" +
                "Subject: Kích hoạt tài khoản Doanh nghiệp WMS\n" +
                "Body:\n" +
                "Xin chào quý khách,\n" +
                "Vui lòng nhấp vào liên kết sau để kích hoạt không gian kho của bạn:\n" +
                "{VerificationLink}\n" +
                "================================================================================\n",
                toName, toEmail, verificationLink);

            return;
        }

        try
        {
            _logger.LogInformation("Đang gửi email xác thực tới {ToEmail} qua máy chủ SMTP {SmtpHost}:{SmtpPort}...",
                toEmail, emailConfig.SmtpHost, emailConfig.SmtpPort);

            using var client = CreateSmtpClient(emailConfig);
            var sender = !string.IsNullOrWhiteSpace(emailConfig.SenderEmail) ? emailConfig.SenderEmail : emailConfig.Username;
            using var mail = new MailMessage
            {
                From = new MailAddress(sender, emailConfig.SenderName),
                Subject = "Kích hoạt tài khoản Doanh nghiệp - Nền tảng WMS",
                IsBodyHtml = true,
                Body = BuildVerificationHtmlBody(toName, verificationLink)
            };

            mail.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(mail);
            _logger.LogInformation("Đã gửi thành công email xác thực tới {ToEmail}.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email xác thực qua SMTP tới {ToEmail}: {Message}", toEmail, ex.Message);
            throw new InvalidOperationException($"Không thể gửi email xác thực tới địa chỉ {toEmail}. Vui lòng thử lại sau.", ex);
        }
    }

    public async Task SendAlertEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var emailConfig = _options.Value;

        if (IsSimulated(emailConfig))
        {
            _logger.LogInformation(
                "\n================================================================================\n" +
                "[SIMULATED EMAIL ALERT DISPATCH]\n" +
                "To: {ToName} <{ToEmail}>\n" +
                "Subject: {Subject}\n" +
                "Body:\n" +
                "{Body}\n" +
                "================================================================================\n",
                toName, toEmail, subject, htmlBody);

            return;
        }

        try
        {
            using var client = CreateSmtpClient(emailConfig);
            var sender = !string.IsNullOrWhiteSpace(emailConfig.SenderEmail) ? emailConfig.SenderEmail : emailConfig.Username;
            using var mail = new MailMessage
            {
                From = new MailAddress(sender, emailConfig.SenderName),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody
            };

            mail.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(mail);
            _logger.LogInformation("Đã gửi thành công email cảnh báo tới {ToEmail}.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi email cảnh báo tới {ToEmail}: {Message}", toEmail, ex.Message);
            // Non-blocking: Do not rethrow
        }
    }

    public async Task<bool> TestEmailConnectionAsync(string testToEmail)
    {
        var emailConfig = _options.Value;

        if (IsSimulated(emailConfig))
        {
            _logger.LogInformation(
                "\n[SIMULATED TEST EMAIL] Gửi mail thử nghiệm tới {TestEmail} thành công ở chế độ Simulated.\n",
                testToEmail);
            await Task.CompletedTask;
            return true;
        }

        try
        {
            using var client = CreateSmtpClient(emailConfig);
            var sender = !string.IsNullOrWhiteSpace(emailConfig.SenderEmail) ? emailConfig.SenderEmail : emailConfig.Username;
            using var mail = new MailMessage
            {
                From = new MailAddress(sender, emailConfig.SenderName),
                Subject = "[WMS Test] Kiểm tra kết nối máy chủ gửi thư cảnh báo",
                IsBodyHtml = true,
                Body = "<p>Xin chào,</p><p>Đây là email kiểm tra kết nối SMTP tự động từ hệ thống WMS Cloud Platform. Cấu hình gửi mail đang hoạt động chính xác!</p>"
            };

            mail.To.Add(new MailAddress(testToEmail));
            await client.SendMailAsync(mail);
            _logger.LogInformation("Đã gửi thử nghiệm email thành công tới {TestEmail}.", testToEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kiểm tra kết nối SMTP thất bại tới {TestEmail}: {Message}", testToEmail, ex.Message);
            throw new InvalidOperationException($"Lỗi kết nối máy chủ SMTP: {ex.Message}", ex);
        }
    }

    private static SmtpClient CreateSmtpClient(EmailOptions emailConfig)
    {
        return new SmtpClient(emailConfig.SmtpHost, emailConfig.SmtpPort)
        {
            EnableSsl = emailConfig.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(emailConfig.Username, emailConfig.Password),
            Timeout = 10000 // 10s non-blocking ergonomics
        };
    }

    private static string BuildVerificationHtmlBody(string recipientName, string link)
    {
        return $@"
            <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 32px 20px; background-color: #F8FAFC;"">
                <div style=""background-color: #FFFFFF; border-radius: 12px; border: 1px solid #E2E8F0; padding: 36px 32px; box-shadow: 0 4px 12px rgba(0,0,0,0.05);"">
                    <div style=""margin-bottom: 24px;"">
                        <span style=""display: inline-block; background-color: #1677FF; color: #FFFFFF; font-weight: bold; font-size: 16px; padding: 6px 14px; border-radius: 6px; letter-spacing: 1px;"">WMS PLATFORM</span>
                    </div>
                    <h2 style=""color: #0F172A; font-size: 22px; font-weight: 700; margin-top: 0; margin-bottom: 16px;"">Xác thực tài khoản Doanh nghiệp</h2>
                    <p style=""color: #334155; font-size: 15px; line-height: 1.6; margin-bottom: 20px;"">
                        Xin chào <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,
                    </p>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6; margin-bottom: 28px;"">
                        Cảm ơn bạn đã đăng ký tài khoản doanh nghiệp trên nền tảng WMS. Để kích hoạt không gian kho và bắt đầu sử dụng, vui lòng bấm vào nút xác nhận bên dưới:
                    </p>
                    <div style=""text-align: center; margin-bottom: 32px;"">
                        <a href=""{link}"" style=""display: inline-block; background-color: #1677FF; color: #FFFFFF; font-size: 16px; font-weight: 600; text-decoration: none; padding: 14px 32px; border-radius: 8px; box-shadow: 0 4px 10px rgba(22, 119, 255, 0.3);"">
                            Xác thực Email & Kích hoạt tài khoản
                        </a>
                    </div>
                    <p style=""color: #64748B; font-size: 13px; line-height: 1.5; margin-bottom: 8px;"">
                        Nếu nút trên không hoạt động, bạn có thể sao chép liên kết sau và dán vào thanh địa chỉ của trình duyệt:
                    </p>
                    <div style=""background-color: #F1F5F9; border-radius: 6px; padding: 10px; word-break: break-all; font-family: monospace; font-size: 12px; color: #0284C7; margin-bottom: 24px;"">
                        {link}
                    </div>
                    <hr style=""border: none; border-top: 1px solid #E2E8F0; margin: 24px 0;"" />
                    <p style=""color: #94A3B8; font-size: 12px; margin: 0; text-align: center;"">
                        Email này được gửi tự động từ hệ thống WMS Cloud Platform. Vui lòng không trả lời trực tiếp email này.
                    </p>
                </div>
            </div>";
    }
}
