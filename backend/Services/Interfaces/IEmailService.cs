namespace BoostingHub.backend.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendWelcomeEmailAsync(string to, string? name);
    Task SendEmailVerificationAsync(string to, string token, string? name);
    Task SendPasswordResetAsync(string to, string token, string? name);
    Task SendAccountLockedAsync(string to, string? name, TimeSpan lockoutDuration);
    Task SendPasswordChangedAsync(string to, string? name);
    Task SendMfaCodeAsync(string to, string code);
}
