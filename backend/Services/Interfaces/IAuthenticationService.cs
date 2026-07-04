using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IAuthenticationService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, HttpContext httpContext, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, HttpContext httpContext, CancellationToken ct = default);
    Task<Result> LogoutAsync(int userId, string? sessionId = null, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default);
    Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task<Result> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken ct = default);
    Task<Result<UserDto>> GetCurrentUserAsync(int userId, CancellationToken ct = default);
}
