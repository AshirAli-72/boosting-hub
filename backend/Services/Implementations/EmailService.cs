using System.Net;
using System.Net.Mail;
using BoostingHub.backend.Services.Interfaces;

namespace BoostingHub.backend.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "";
        var smtpPortStr = _config["Email:SmtpPort"] ?? "587";
        var smtpUser = _config["Email:SmtpUser"] ?? "";
        var smtpPass = "leir wkry bklp szmt";
        var fromEmail = _config["Email:FromEmail"] ?? "noreply@boostinghub.com";
        var fromName = _config["Email:FromName"] ?? "Boosting Hub";

        if (!string.IsNullOrEmpty(smtpHost))
        {
            try
            {
                using var client = new SmtpClient(smtpHost, int.Parse(smtpPortStr))
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };
                var mailMsg = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMsg.To.Add(to);
                await client.SendMailAsync(mailMsg);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {To}: {Subject}. Body:\n{Body}", to, subject, body);
            }
        }
        else
        {
            _logger.LogWarning("SMTP not configured. Email NOT sent to {To}: {Subject}", to, subject);
            _logger.LogWarning("--- Email Body Preview ---\n{Body}\n--- End ---", body);
            await Task.CompletedTask;
        }
    }

    public async Task SendWelcomeEmailAsync(string to, string? name)
    {
        var body = GetTemplate("Welcome", name);
        await SendEmailAsync(to, "Welcome to Boosting Hub!", body);
    }

    public async Task SendEmailVerificationAsync(string to, string token, string? name)
    {
        var body = GetTemplate("VerifyEmail", name, token);
        await SendEmailAsync(to, "Verify Your Email", body);
    }

    public async Task SendPasswordResetAsync(string to, string token, string? name)
    {
        var body = GetTemplate("ResetPassword", name, token);
        await SendEmailAsync(to, "Reset Your Password", body);
    }

    public async Task SendAccountLockedAsync(string to, string? name, TimeSpan lockoutDuration)
    {
        var body = GetTemplate("AccountLocked", name, lockoutDuration.ToString());
        await SendEmailAsync(to, "Account Locked", body);
    }

    public async Task SendPasswordChangedAsync(string to, string? name)
    {
        var body = GetTemplate("PasswordChanged", name);
        await SendEmailAsync(to, "Password Changed Successfully", body);
    }

    public async Task SendMfaCodeAsync(string to, string code)
    {
        await SendEmailAsync(to, "Your MFA Code", $"Your verification code is: {code}");
    }

    private string GetTemplate(string templateName, string? name, string? data = null)
    {
        var brandColor = "#7C3AED";
        var accentColor = "#0D9488";
        var bgColor = "#F8FAFC";
        var cardBg = "#FFFFFF";
        var textColor = "#1E293B";
        var mutedColor = "#64748B";

        var bodyContent = templateName switch
        {
            "Welcome" => $"""
            <p style="font-size:16px;color:{textColor};line-height:1.7;">Welcome to <strong>Boosting Hub</strong>! We're thrilled to have you on board.</p>
            <p style="font-size:15px;color:{textColor};line-height:1.7;">Get started by browsing available tasks and earning rewards right away.</p>
            <div style="text-align:center;margin:32px 0;">
                <a href="{data}" style="display:inline-block;padding:14px 36px;font-size:16px;font-weight:600;color:#fff;background:linear-gradient(135deg,{brandColor},{accentColor});border-radius:10px;text-decoration:none;">Get Started</a>
            </div>
            """,
            "VerifyEmail" => $"""
            <p style="font-size:16px;color:{textColor};line-height:1.7;">Thanks for signing up! Please verify your email address by clicking the button below.</p>
            <div style="text-align:center;margin:32px 0;">
                <a href="{data}" style="display:inline-block;padding:14px 36px;font-size:16px;font-weight:600;color:#fff;background:linear-gradient(135deg,{brandColor},{accentColor});border-radius:10px;text-decoration:none;">Verify Email Address</a>
            </div>
            <p style="font-size:14px;color:{mutedColor};text-align:center;">This link expires in <strong>24 hours</strong>.</p>
            <p style="font-size:14px;color:{mutedColor};text-align:center;">If you didn't create an account, you can safely ignore this email.</p>
            """,
            "ResetPassword" => $"""
            <p style="font-size:16px;color:{textColor};line-height:1.7;">We received a request to reset your password. Click the button below to set a new one.</p>
            <div style="text-align:center;margin:32px 0;">
                <a href="{data}" style="display:inline-block;padding:14px 36px;font-size:16px;font-weight:600;color:#fff;background:linear-gradient(135deg,{brandColor},{accentColor});border-radius:10px;text-decoration:none;">Reset Password</a>
            </div>
            <p style="font-size:14px;color:{mutedColor};text-align:center;">This link expires in <strong>1 hour</strong>.</p>
            <p style="font-size:14px;color:{mutedColor};text-align:center;">If you didn't request this, you can safely ignore this email.</p>
            """,
            "AccountLocked" => $"""
            <p style="font-size:16px;color:{textColor};line-height:1.7;">Your account has been locked due to multiple failed login attempts.</p>
            <p style="font-size:15px;color:{textColor};line-height:1.7;">It will automatically unlock in <strong>{data}</strong>.</p>
            <p style="font-size:14px;color:{mutedColor};text-align:center;">If you need immediate assistance, please contact support.</p>
            """,
            "PasswordChanged" => $"""
            <p style="font-size:16px;color:{textColor};line-height:1.7;">Your password has been changed successfully.</p>
            <p style="font-size:14px;color:{mutedColor};text-align:center;">If you didn't make this change, please contact support immediately.</p>
            """,
            _ => $"""
            <p style="font-size:16px;color:{textColor};line-height:1.7;">Notification from Boosting Hub.</p>
            """
        };

        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"></head>
        <body style="margin:0;padding:0;background-color:{bgColor};font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;">
            <table role="presentation" style="width:100%;border-collapse:collapse;">
                <tr><td style="padding:40px 16px;">
                    <table role="presentation" style="max-width:560px;margin:0 auto;background:{cardBg};border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,0.08);border-collapse:collapse;overflow:hidden;">
                        <tr>
                            <td style="background:linear-gradient(135deg,{brandColor},{accentColor});padding:32px 40px;text-align:center;">
                                <div style="width:56px;height:56px;background:rgba(255,255,255,0.18);border-radius:14px;display:inline-flex;align-items:center;justify-content:center;margin-bottom:12px;">
                                    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>
                                </div>
                                <h1 style="margin:0;font-size:22px;font-weight:700;color:#fff;letter-spacing:-0.3px;">Boosting Hub</h1>
                            </td>
                        </tr>
                        <tr><td style="padding:36px 40px 28px;">
                            <p style="font-size:15px;color:{mutedColor};margin:0 0 4px;">Hello {name ?? "User"},</p>
                            {bodyContent}
                        </td></tr>
                        <tr>
                            <td style="padding:20px 40px;background:{bgColor};border-top:1px solid #E2E8F0;text-align:center;">
                                <p style="margin:0;font-size:13px;color:{mutedColor};">&copy; 2026 Boosting Hub. All rights reserved.</p>
                                <p style="margin:4px 0 0;font-size:12px;color:#94A3B8;">If you have questions, contact our support team.</p>
                            </td>
                        </tr>
                    </table>
                </td></tr>
            </table>
        </body>
        </html>
        """;
    }
}
