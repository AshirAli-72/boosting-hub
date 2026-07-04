using BoostingHub.backend.Services.Interfaces;

namespace BoostingHub.backend.Services.Implementations;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("SMS sent to {Phone}: {Message}", phoneNumber, message);
        await Task.CompletedTask;
    }

    public async Task SendOtpAsync(string phoneNumber, string otpCode)
    {
        await SendSmsAsync(phoneNumber, $"Your Boosting Hub verification code is: {otpCode}. It expires in 10 minutes.");
    }

    public async Task SendLoginVerificationAsync(string phoneNumber, string otpCode)
    {
        await SendSmsAsync(phoneNumber, $"Your Boosting Hub login code is: {otpCode}. Do not share this with anyone.");
    }
}
