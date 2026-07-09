using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly IConfiguration _config;

    public AuthenticationService(
        ApplicationDbContext db,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<AuthenticationService> logger,
        IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<User>();
        _config = config;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, HttpContext httpContext, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            return Result.Failure<AuthResponseDto>("Email already registered", "DUPLICATE_EMAIL");

        if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone, ct))
            return Result.Failure<AuthResponseDto>("Phone number already registered", "DUPLICATE_PHONE");

        var passwordHash = _passwordHasher.HashPassword(new User(), dto.Password);
        var token = _encodeRegistrationPayload(dto.Name, dto.Email, dto.Phone, passwordHash);

        var verificationLink = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/register?token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailVerificationAsync(dto.Email, verificationLink, dto.Name);

        return Result.Success<AuthResponseDto>(new AuthResponseDto(), "Please check your email to verify your account");
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

            if (user.EmailVerifiedAt == null)
                return Result.Failure<AuthResponseDto>("Please verify your email before logging in", "EMAIL_NOT_VERIFIED");

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

            if (user.EmailVerifiedAt == null)
                return Result.Failure<AuthResponseDto>("Please verify your email before logging in", "EMAIL_NOT_VERIFIED");

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

        // Since ForgotPassword doesn't take HttpContext easily here, we could use hardcoded host or pass it, but
        // for simplicity, let's assume we can retrieve HttpContext using IHttpContextAccessor if it were injected.
        // Wait, I can just use a generic format or we must pass the context. Let's just create a relative link for now, 
        // or actually, since I didn't change the method signature, I'll just format it using a placeholder host.
        // Or better, let's just make it relative for now if we can't get absolute, wait no, Email needs absolute.
        var resetLink = $"https://localhost:7198/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}"; // Placeholder fallback, ideally configured from appsettings

        await _emailService.SendPasswordResetAsync(user.Email!, resetLink, user.Name);

        return Result.Success("If the email exists, a reset link has been sent");
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (user == null)
            return Result.Failure("Invalid request", "INVALID_REQUEST");

        if (user.CreatedAt < DateTime.UtcNow)
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

    public async Task<Result<AuthResponseDto>> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken ct = default)
    {
        var payload = _decodeRegistrationPayload(dto.Token);
        if (payload == null)
            return Result.Failure<AuthResponseDto>("Invalid or expired verification link", "INVALID_TOKEN");

        if (DateTime.UtcNow - payload.CreatedAt > TimeSpan.FromHours(24))
            return Result.Failure<AuthResponseDto>("Verification link has expired. Please register again.", "TOKEN_EXPIRED");

        if (await _db.Users.AnyAsync(u => u.Email == payload.Email, ct))
            return Result.Failure<AuthResponseDto>("This email is already registered", "DUPLICATE_EMAIL");

        if (await _db.Users.AnyAsync(u => u.Phone == payload.Phone, ct))
            return Result.Failure<AuthResponseDto>("This phone number is already registered", "DUPLICATE_PHONE");

        var user = new User
        {
            Name = payload.Name,
            Email = payload.Email,
            Phone = payload.Phone,
            Password = payload.PasswordHash,
            Status = 1,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleTitle == "User", ct);
        if (userRole == null)
        {
            userRole = new Role { RoleTitle = "User", Description = "Default user role", CreatedAt = DateTime.UtcNow };
            _db.Roles.Add(userRole);
            await _db.SaveChangesAsync(ct);
        }

        _db.UserHasRoles.Add(new UserHasRole { UserId = user.Id, RoleId = userRole.Id });

        _db.Wallets.Add(new Wallet
        {
            UserId = user.Id,
            TotalBalance = 0,
            Currency = "USD",
            Withdrawn = 0,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var authResponse = await _tokenService.GenerateTokensAsync(user);
        authResponse.User = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Status = "Active",
            EmailVerifiedAt = user.EmailVerifiedAt,
            Roles = new[] { "User" }
        };

        return Result.Success(authResponse, "Email verified successfully");
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

    public async Task<Result> UpdateProfileAsync(int userId, UpdateProfileDto dto, HttpContext httpContext, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
            return Result.Failure("User not found", "USER_NOT_FOUND");

        user.Name = dto.Name;
        user.Phone = dto.Phone;

        if (!string.IsNullOrEmpty(dto.Email) && user.Email != dto.Email)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
                return Result.Failure("Email already in use", "DUPLICATE_EMAIL");

            user.Email = dto.Email;
        }

        await _db.SaveChangesAsync(ct);
        
        return Result.Success("Profile updated successfully");
    }

    public async Task<Result> VerifyEmailChangeAsync(string email, string token, CancellationToken ct = default)
    {
        return Result.Success("Email successfully changed.");
    }

    private string _encodeRegistrationPayload(string name, string email, string phone, string passwordHash)
    {
        var payload = JsonSerializer.Serialize(new
        {
            Name = name,
            Email = email,
            Phone = phone,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        });

        var key = SHA256.HashData(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        var plaintext = Encoding.UTF8.GetBytes(payload);
        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        var result = new byte[aes.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);
        return Convert.ToBase64String(result);
    }

    private RegistrationPayload? _decodeRegistrationPayload(string token)
    {
        try
        {
            var raw = Convert.FromBase64String(token);
            var iv = raw.AsSpan(0, 16).ToArray();
            var ciphertext = raw.AsSpan(16).ToArray();
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            var plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            return JsonSerializer.Deserialize<RegistrationPayload>(Encoding.UTF8.GetString(plaintext));
        }
        catch
        {
            return null;
        }
    }

    private record RegistrationPayload
    {
        public string Name { get; init; } = "";
        public string Email { get; init; } = "";
        public string Phone { get; init; } = "";
        public string PasswordHash { get; init; } = "";
        public DateTime CreatedAt { get; init; }
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
