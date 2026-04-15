using System.Net;
using System.Net.Mail;
using System.Text;

namespace NextHorizon.Services;

public sealed class EmailService : IEmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _isConfigured;

    public EmailService(IConfiguration configuration)
    {
        _smtpServer = configuration["EmailSettings:SmtpServer"] ?? string.Empty;
        _smtpUsername = configuration["EmailSettings:SmtpUsername"] ?? string.Empty;
        _smtpPassword = configuration["EmailSettings:SmtpPassword"] ?? string.Empty;
        _fromEmail = configuration["EmailSettings:FromEmail"] ?? string.Empty;
        _fromName = configuration["EmailSettings:FromName"] ?? "Next Horizon";
        _smtpPort = int.TryParse(configuration["EmailSettings:SmtpPort"], out var smtpPort) ? smtpPort : 587;

        _isConfigured =
            !string.IsNullOrWhiteSpace(_smtpServer) &&
            !string.IsNullOrWhiteSpace(_smtpUsername) &&
            !string.IsNullOrWhiteSpace(_smtpPassword) &&
            !string.IsNullOrWhiteSpace(_fromEmail);
    }

    public Task<bool> SendOTPEmailAsync(string email, string name, string otpCode)
    {
        var body = BuildEmailBody(
            "Password Reset OTP",
            $"""
             <p>Hello {WebUtility.HtmlEncode(name)},</p>
             <p>Use the OTP below to continue your password reset request for the QA workspace.</p>
             <p style="font-size: 28px; font-weight: 700; letter-spacing: 6px; margin: 24px 0;">{WebUtility.HtmlEncode(otpCode)}</p>
             <p>This code expires in 15 minutes.</p>
             """);

        return SendEmailAsync(email, "Next Horizon QA password reset OTP", body);
    }

    public Task<bool> SendPasswordResetConfirmationAsync(string email, string name)
    {
        var body = BuildEmailBody(
            "Password Updated",
            $"""
             <p>Hello {WebUtility.HtmlEncode(name)},</p>
             <p>Your QA workspace password has been reset successfully.</p>
             <p>If you did not make this change, contact support immediately.</p>
             """);

        return SendEmailAsync(email, "Next Horizon QA password updated", body);
    }

    public Task<bool> SendAccountDeletionEmailAsync(string email, string name, string adminName)
    {
        var body = BuildEmailBody(
            "Account Disabled",
            $"""
             <p>Hello {WebUtility.HtmlEncode(name)},</p>
             <p>Your account has been disabled by {WebUtility.HtmlEncode(adminName)}.</p>
             """);

        return SendEmailAsync(email, "Next Horizon account disabled", body);
    }

    public Task<bool> SendAccountRestoreEmailAsync(string email, string name, string adminName)
    {
        var body = BuildEmailBody(
            "Account Restored",
            $"""
             <p>Hello {WebUtility.HtmlEncode(name)},</p>
             <p>Your account has been restored by {WebUtility.HtmlEncode(adminName)}.</p>
             """);

        return SendEmailAsync(email, "Next Horizon account restored", body);
    }

    public Task<bool> SendSellerStatusUpdateEmailAsync(string email, string businessName, string status, string note, string adminName)
    {
        var body = BuildEmailBody(
            "Seller Status Update",
            $"""
             <p>Business: {WebUtility.HtmlEncode(businessName)}</p>
             <p>Status: {WebUtility.HtmlEncode(status)}</p>
             <p>Updated by: {WebUtility.HtmlEncode(adminName)}</p>
             <p>Note: {WebUtility.HtmlEncode(note)}</p>
             """);

        return SendEmailAsync(email, "Next Horizon seller status update", body);
    }

    public Task<bool> SendAdminCredentialsEmailAsync(string email, string firstName, string lastName, string username, string password, string userType, string addedByAdmin)
    {
        var fullName = string.Join(' ', new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var body = BuildEmailBody(
            "Admin Credentials",
            $"""
             <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
             <p>You were added as {WebUtility.HtmlEncode(userType)} by {WebUtility.HtmlEncode(addedByAdmin)}.</p>
             <p>Username: <strong>{WebUtility.HtmlEncode(username)}</strong></p>
             <p>Password: <strong>{WebUtility.HtmlEncode(password)}</strong></p>
             """);

        return SendEmailAsync(email, "Next Horizon admin credentials", body);
    }

    public Task<bool> SendAdminRevokedEmailAsync(string email, string firstName, string lastName, string userType, string reason, string revokedByAdmin)
    {
        var fullName = string.Join(' ', new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var body = BuildEmailBody(
            "Admin Access Revoked",
            $"""
             <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
             <p>Your {WebUtility.HtmlEncode(userType)} access was revoked by {WebUtility.HtmlEncode(revokedByAdmin)}.</p>
             <p>Reason: {WebUtility.HtmlEncode(reason)}</p>
             """);

        return SendEmailAsync(email, "Next Horizon admin access revoked", body);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        if (!_isConfigured)
        {
            return false;
        }

        try
        {
            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName, Encoding.UTF8),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildEmailBody(string title, string content)
    {
        return $"""
                <html>
                <body style="margin:0;padding:24px;background:#f4f4f4;font-family:Arial,sans-serif;color:#111;">
                    <div style="max-width:560px;margin:0 auto;background:#fff;border:1px solid #ddd;border-radius:16px;overflow:hidden;">
                        <div style="background:#111;color:#fff;padding:24px;text-align:center;">
                            <div style="font-size:32px;font-weight:700;letter-spacing:1px;">NH</div>
                            <div style="font-size:12px;letter-spacing:3px;text-transform:uppercase;opacity:.8;">Next Horizon</div>
                        </div>
                        <div style="padding:32px 28px;">
                            <h2 style="margin:0 0 16px;font-size:24px;">{WebUtility.HtmlEncode(title)}</h2>
                            {content}
                        </div>
                    </div>
                </body>
                </html>
                """;
    }
}
