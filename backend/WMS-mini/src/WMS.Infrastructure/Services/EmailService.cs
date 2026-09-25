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

    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verificationLink)
    {
        var emailConfig = _options.Value;

        // Chế độ 1: Simulated (Dành cho Development / Local)
        if (string.Equals(emailConfig.Provider, "Simulated", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(emailConfig.SmtpHost))
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

        // Chế độ 2: SMTP Thật (Hỗ trợ Gmail, Outlook, Lark Suite, Custom SMTP)
        try
        {
            _logger.LogInformation("Đang gửi email xác thực tới {ToEmail} qua máy chủ SMTP {SmtpHost}:{SmtpPort}...",
                toEmail, emailConfig.SmtpHost, emailConfig.SmtpPort);

            using var client = new SmtpClient(emailConfig.SmtpHost, emailConfig.SmtpPort)
            {
                EnableSsl = emailConfig.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(emailConfig.Username, emailConfig.Password),
                Timeout = 20000 // 20s
            };

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
