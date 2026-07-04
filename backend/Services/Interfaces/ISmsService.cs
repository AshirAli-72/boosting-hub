namespace BoostingHub.backend.Services.Interfaces;

public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string message);
    Task SendOtpAsync(string phoneNumber, string otpCode);
    Task SendLoginVerificationAsync(string phoneNumber, string otpCode);
}
