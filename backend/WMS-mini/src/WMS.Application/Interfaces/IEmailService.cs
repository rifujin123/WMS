namespace WMS.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string toName, string verificationLink);
    Task SendAlertEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    Task<bool> TestEmailConnectionAsync(string testToEmail);
}
