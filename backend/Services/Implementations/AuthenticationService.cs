using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthenticationService(
        ApplicationDbContext db,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<AuthenticationService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, HttpContext httpContext, CancellationToken ct = default)
    {
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (existingUser != null)
            return Result.Failure<AuthResponseDto>("Email already registered", "DUPLICATE_EMAIL");

        if (!string.IsNullOrEmpty(dto.Phone))
        {
            var existingPhone = await _db.Users.AnyAsync(u => u.Phone == dto.Phone, ct);
            if (existingPhone)
                return Result.Failure<AuthResponseDto>("Phone number already registered", "DUPLICATE_PHONE");
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Status = 1,
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        user.Password = _passwordHasher.HashPassword(user, dto.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var guestRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleTitle == "Guest", ct);
        if (guestRole == null)
        {
            guestRole = new Role { RoleTitle = "Guest", Description = "Guest role", CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow) };
            _db.Roles.Add(guestRole);
            await _db.SaveChangesAsync(ct);
        }

        _db.UserHasRoles.Add(new UserHasRole { UserId = user.Id, RoleId = guestRole.Id });
        await _db.SaveChangesAsync(ct);

        //var emailToken = _generateSecureToken();
        //user.RememberToken = _hashToken(emailToken);
        //await _db.SaveChangesAsync(ct);

        //await _emailService.SendWelcomeEmailAsync(user.Email!, user.Name);
        //await _emailService.SendEmailVerificationAsync(user.Email!, emailToken, user.Name);

        var authResponse = await _tokenService.GenerateTokensAsync(user);

        // Populate User on registration (same as login path) so Register page can store session
        var userWithRoles = await _db.Users
            .Include(u => u.UserHasRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == user.Id, ct);

        authResponse.User = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Status = user.Status == 1 ? "Active" : "Inactive",
            EmailVerifiedAt = user.EmailVerifiedAt,
            Roles = userWithRoles!.UserHasRoles.Select(ur => ur.Role!.RoleTitle).ToArray()
        };

        return Result.Success(authResponse, "Registration successful.");
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, HttpContext httpContext, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(dto.Email))
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
            if (user == null)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            if (user.Status != 1)
                return Result.Failure<AuthResponseDto>("Account is deactivated", "ACCOUNT_DEACTIVATED");

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password!, dto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            var userWithRoles = await _db.Users.Include(u => u.UserHasRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            var authResponse = await _tokenService.GenerateTokensAsync(user);
            authResponse.User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Status = user.Status == 1 ? "Active" : "Inactive",
                EmailVerifiedAt = user.EmailVerifiedAt,
                Roles = userWithRoles!.UserHasRoles.Select(ur => ur.Role!.RoleTitle).ToArray()
            };

            return Result.Success(authResponse, "Login successful");
        }

        if (!string.IsNullOrEmpty(dto.Phone))
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == dto.Phone, ct);
            if (user == null)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            if (user.Status != 1)
                return Result.Failure<AuthResponseDto>("Account is deactivated", "ACCOUNT_DEACTIVATED");

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password!, dto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            var userWithRoles = await _db.Users.Include(u => u.UserHasRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            var authResponse = await _tokenService.GenerateTokensAsync(user);
            authResponse.User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Status = user.Status == 1 ? "Active" : "Inactive",
                EmailVerifiedAt = user.EmailVerifiedAt,
                Roles = userWithRoles!.UserHasRoles.Select(ur => ur.Role!.RoleTitle).ToArray()
            };

            return Result.Success(authResponse, "Login successful");
        }

        return Result.Failure<AuthResponseDto>("Email or phone is required", "INVALID_INPUT");
    }

    public async Task<Result> LogoutAsync(int userId, string? sessionId = null, CancellationToken ct = default)
    {
        return Result.Success("Logged out successfully");
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal == null)
            return Result.Failure<AuthResponseDto>("Invalid access token", "INVALID_TOKEN");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Result.Failure<AuthResponseDto>("Invalid token payload", "INVALID_TOKEN");

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null || user.Status != 1)
            return Result.Failure<AuthResponseDto>("User not found or inactive", "USER_NOT_FOUND");

        var authResponse = await _tokenService.GenerateTokensAsync(user);

        return Result.Success(authResponse, "Token refreshed");
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (user == null)
            return Result.Success("If the email exists, a reset link has been sent");

        var token = _generateSecureToken();
        user.RememberToken = _hashToken(token);
        await _db.SaveChangesAsync(ct);

        await _emailService.SendPasswordResetAsync(user.Email!, token, user.Name);

        return Result.Success("If the email exists, a reset link has been sent");
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (user == null)
            return Result.Failure("Invalid request", "INVALID_REQUEST");

        if (user.CreatedAt < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure("Token expired", "TOKEN_EXPIRED");

        var tokenHash = _hashToken(dto.Token);
        if (user.RememberToken == null || user.RememberToken != tokenHash)
            return Result.Failure("Invalid token", "INVALID_TOKEN");

        user.Password = _passwordHasher.HashPassword(user, dto.Password);
        user.RememberToken = null;
        await _db.SaveChangesAsync(ct);

        await _emailService.SendPasswordChangedAsync(user.Email!, user.Name);

        return Result.Success("Password reset successfully");
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
            return Result.Failure("User not found", "USER_NOT_FOUND");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password!, dto.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Result.Failure("Current password is incorrect", "INVALID_PASSWORD");

        user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync(ct);

        await _emailService.SendPasswordChangedAsync(user.Email!, user.Name);

        return Result.Success("Password changed successfully");
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken ct = default)
    {
        // Verification process commented out
        await Task.CompletedTask;
        return Result.Success("Email verified successfully");
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserHasRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            return Result.Failure<UserDto>("User not found", "USER_NOT_FOUND");

        var dto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Status = user.Status == 1 ? "Active" : "Inactive",
            EmailVerifiedAt = user.EmailVerifiedAt,
            Roles = user.UserHasRoles.Select(ur => ur.Role!.RoleTitle).ToArray()
        };
        return Result.Success(dto);
    }

    private string _generateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string _hashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
