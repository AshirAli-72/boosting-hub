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
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        await Task.CompletedTask;
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
        return $"""
        <!DOCTYPE html>
        <html><body style="font-family:Arial;max-width:600px;margin:auto;padding:20px;">
        <h2 style="color:#6C63FF;">Boosting Hub</h2>
        <p>Hello {name ?? "User"},</p>
        {templateName switch
        {
            "Welcome" => "<p>Welcome to Boosting Hub! We're excited to have you on board.</p>",
            "VerifyEmail" => $"<p>Please verify your email using this link: <a href='{data}'>Verify Email</a></p><p>This link expires in 24 hours.</p>",
            "ResetPassword" => $"<p>Reset your password using this link: <a href='{data}'>Reset Password</a></p><p>This link expires in 1 hour.</p>",
            "AccountLocked" => $"<p>Your account has been locked due to multiple failed attempts. It will unlock in {data}.</p>",
            "PasswordChanged" => "<p>Your password has been changed successfully. If you didn't make this change, please contact support.</p>",
            _ => "<p>Notification from Boosting Hub.</p>"
        }}
        <hr><p style="color:#888;font-size:12px;">Boosting Hub &copy; 2026</p>
        </body></html>
        """;
    }
}
