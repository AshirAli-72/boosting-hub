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
    private readonly IWebHostEnvironment _env;
    private readonly IActivityLogService _activityLog;
    private readonly INotificationService _notificationService;

    public AuthenticationService(
        ApplicationDbContext db,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<AuthenticationService> logger,
        IConfiguration config,
        IWebHostEnvironment env,
        IActivityLogService activityLog,
        INotificationService notificationService)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<User>();
        _config = config;
        _env = env;
        _activityLog = activityLog;
        _notificationService = notificationService;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, HttpContext httpContext, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            return Result.Failure<AuthResponseDto>("Email already registered", "DUPLICATE_EMAIL");

        if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone, ct))
            return Result.Failure<AuthResponseDto>("Phone number already registered", "DUPLICATE_PHONE");

        var passwordHash = _passwordHasher.HashPassword(new User(), dto.Password);

        // ── Local (Development): create user directly, no email needed ──
        if (_env.IsDevelopment())
        {
            return await _registerDirectlyAsync(dto, passwordHash, httpContext, ct);
        }

        // ── Live (Production): send verification email ──
        var token = _encodeRegistrationPayload(dto.Name, dto.Email, dto.Phone, passwordHash);
        var baseUrl = $"https://{_config["App:Domain"] ?? "boostinghub.somee.com"}";
        var verificationLink = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(token)}";

        try
        {
            await _emailService.SendEmailVerificationAsync(dto.Email, verificationLink, dto.Name);
            return Result.Success<AuthResponseDto>(new AuthResponseDto(), "Please check your email to verify your account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP FAILED for {Email}: {Message}", dto.Email, ex.Message);
            return Result.Failure<AuthResponseDto>($"Email send failed: {ex.Message}", "EMAIL_SEND_FAILED");
        }
    }

    private async Task<Result<AuthResponseDto>> _registerDirectlyAsync(RegisterDto dto, string passwordHash, HttpContext httpContext, CancellationToken ct)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Password = passwordHash,
            Status = StatusHelper.UserActive,
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
            Currency = "PKR",
            Withdrawn = 0,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        });

        if (dto.SocialMediaAccounts?.Any() == true)
        {
            foreach (var sm in dto.SocialMediaAccounts.Where(s => !string.IsNullOrWhiteSpace(s.Username)))
            {
                _db.SocialMediaAccounts.Add(new SocialMediaAccount
                {
                    UserId = user.Id,
                    Platform = sm.Platform,
                    Username = sm.Username,
                    ProfileUrl = sm.ProfileUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        await _activityLog.LogAsync(
            userId: user.Id, userName: user.Name, userEmail: user.Email,
            userRole: "User", evt: "Registered", description: $"User {user.Email} registered (local auto-verified)",
            subjectType: "User", subjectId: user.Id, subjectName: user.Name,
            httpContext: httpContext, ct: ct);

        var adminUserIds = await _db.UserHasRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.Role != null && ur.Role.RoleTitle != null && ur.Role.RoleTitle.Contains("Admin"))
            .Select(ur => ur.UserId)
            .ToListAsync(ct);

        if (adminUserIds.Any())
        {
            var notifications = adminUserIds.Select(adminId => new CreateNotificationDto
            {
                UserId = adminId,
                Type = "NewUserRegistered",
                Title = "New User Registered",
                Message = $"{user.Name} has joined BoostingHub.",
                Data = $"{{\"userId\":{user.Id}}}"
            }).ToList();
            await _notificationService.CreateBulkNotificationAsync(notifications);
        }

        var authResponse = await _tokenService.GenerateTokensAsync(user);
        authResponse.User = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Status = StatusHelper.UserStatusToString(user.Status),
            EmailVerifiedAt = user.EmailVerifiedAt,
            Roles = new[] { "User" }
        };

        return Result.Success(authResponse, "Registration successful");
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, HttpContext httpContext, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(dto.Email))
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
            if (user == null)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            if (user.Status != StatusHelper.UserActive)
                return Result.Failure<AuthResponseDto>("Account is deactivated", "ACCOUNT_DEACTIVATED");

            if (user.EmailVerifiedAt == null)
                return Result.Failure<AuthResponseDto>("Please verify your email before logging in", "EMAIL_NOT_VERIFIED");

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password!, dto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            var userWithRoles = await _db.Users.Include(u => u.UserHasRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            var authResponse = await _tokenService.GenerateTokensAsync(user);
            var roleNames = userWithRoles!.UserHasRoles.Select(ur => ur.Role!.RoleTitle).ToArray();
            authResponse.User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Status = StatusHelper.UserStatusToString(user.Status),
                EmailVerifiedAt = user.EmailVerifiedAt,
                Roles = roleNames
            };

            await _activityLog.LogAsync(
                userId: user.Id, userName: user.Name, userEmail: user.Email,
                userRole: string.Join(",", roleNames), evt: "LoggedIn", description: $"User {user.Email} logged in",
                subjectType: "User", subjectId: user.Id, subjectName: user.Name,
                httpContext: httpContext, ct: ct);

            return Result.Success(authResponse, "Login successful");
        }

        if (!string.IsNullOrEmpty(dto.Phone))
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == dto.Phone, ct);
            if (user == null)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            if (user.Status != StatusHelper.UserActive)
                return Result.Failure<AuthResponseDto>("Account is deactivated", "ACCOUNT_DEACTIVATED");

            if (user.EmailVerifiedAt == null)
                return Result.Failure<AuthResponseDto>("Please verify your email before logging in", "EMAIL_NOT_VERIFIED");

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password!, dto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return Result.Failure<AuthResponseDto>("Invalid credentials", "INVALID_CREDENTIALS");

            var userWithRoles = await _db.Users.Include(u => u.UserHasRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            var authResponse = await _tokenService.GenerateTokensAsync(user);
            var roleNames = userWithRoles!.UserHasRoles.Select(ur => ur.Role!.RoleTitle).ToArray();
            authResponse.User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Status = StatusHelper.UserStatusToString(user.Status),
                EmailVerifiedAt = user.EmailVerifiedAt,
                Roles = roleNames
            };

            await _activityLog.LogAsync(
                userId: user.Id, userName: user.Name, userEmail: user.Email,
                userRole: string.Join(",", roleNames), evt: "LoggedIn", description: $"User {user.Email} logged in",
                subjectType: "User", subjectId: user.Id, subjectName: user.Name,
                httpContext: httpContext, ct: ct);

            return Result.Success(authResponse, "Login successful");
        }

        return Result.Failure<AuthResponseDto>("Email or phone is required", "INVALID_INPUT");
    }

    public async Task<Result> LogoutAsync(int userId, string? sessionId = null, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user != null)
        {
            await _activityLog.LogAsync(
                userId: user.Id, userName: user.Name, userEmail: user.Email,
                userRole: "User", evt: "LoggedOut", description: $"User {user.Email} logged out",
                subjectType: "User", subjectId: user.Id, subjectName: user.Name,
                ct: ct);
        }

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

        var domain = _config["App:Domain"] ?? "boostinghub.somee.com";
        var scheme = domain.Contains("localhost") ? "http" : "https";
        var baseUrl = $"{scheme}://{domain}";
        var resetLink = $"{baseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email!, resetLink, user.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        await _activityLog.LogAsync(
            userId: user.Id, userName: user.Name, userEmail: user.Email,
            userRole: "User", evt: "ForgotPassword", description: $"Password reset requested for {user.Email}",
            subjectType: "User", subjectId: user.Id, subjectName: user.Name,
            ct: ct);

        return Result.Success("If the email exists, a reset link has been sent");
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (user == null)
            return Result.Failure("Invalid request", "INVALID_REQUEST");

        var tokenHash = _hashToken(dto.Token);
        if (user.RememberToken == null || user.RememberToken != tokenHash)
            return Result.Failure("Invalid or expired reset link", "INVALID_TOKEN");

        user.Password = _passwordHasher.HashPassword(user, dto.Password);
        user.RememberToken = null;
        await _db.SaveChangesAsync(ct);

        await _emailService.SendPasswordChangedAsync(user.Email!, user.Name);

        await _activityLog.LogAsync(
            userId: user.Id, userName: user.Name, userEmail: user.Email,
            userRole: "User", evt: "PasswordReset", description: $"Password reset completed for {user.Email}",
            subjectType: "User", subjectId: user.Id, subjectName: user.Name,
            ct: ct);

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

        await _activityLog.LogAsync(
            userId: user.Id, userName: user.Name, userEmail: user.Email,
            userRole: "User", evt: "PasswordChanged", description: $"Password changed for {user.Email}",
            subjectType: "User", subjectId: user.Id, subjectName: user.Name,
            ct: ct);

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
            Currency = "PKR",
            Withdrawn = 0,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var adminUserIds = await _db.UserHasRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.Role != null && ur.Role.RoleTitle != null && ur.Role.RoleTitle.Contains("Admin"))
            .Select(ur => ur.UserId)
            .ToListAsync(ct);

        if (adminUserIds.Any())
        {
            var notifications = adminUserIds.Select(adminId => new CreateNotificationDto
            {
                UserId = adminId,
                Type = "NewUserRegistered",
                Title = "New User Registered",
                Message = $"{user.Name} has joined BoostingHub.",
                Data = $"{{\"userId\":{user.Id}}}"
            }).ToList();
            await _notificationService.CreateBulkNotificationAsync(notifications);
        }

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

        var oldName = user.Name;
        var oldEmail = user.Email;
        var oldPhone = user.Phone;

        user.Name = dto.Name;
        user.Phone = dto.Phone;

        if (!string.IsNullOrEmpty(dto.Email) && user.Email != dto.Email)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
                return Result.Failure("Email already in use", "DUPLICATE_EMAIL");

            user.Email = dto.Email;
        }

        await _db.SaveChangesAsync(ct);

        await _activityLog.LogAsync(
            userId: user.Id, userName: user.Name, userEmail: user.Email,
            userRole: "User", evt: "ProfileUpdated", description: $"Profile updated for {user.Email}",
            subjectType: "User", subjectId: user.Id, subjectName: user.Name,
            oldValues: JsonSerializer.Serialize(new { Name = oldName, Email = oldEmail, Phone = oldPhone }),
            newValues: JsonSerializer.Serialize(new { Name = dto.Name, Email = dto.Email, Phone = dto.Phone }),
            httpContext: httpContext, ct: ct);
        
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

    public async Task<Result<List<SocialMediaAccountDto>>> GetSocialMediaAccountsAsync(int userId, CancellationToken ct = default)
    {
        var accounts = await _db.SocialMediaAccounts
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Platform)
            .Select(s => new SocialMediaAccountDto
            {
                Id = s.Id,
                Platform = s.Platform,
                Username = s.Username,
                ProfileUrl = s.ProfileUrl
            })
            .ToListAsync(ct);

        return Result.Success(accounts);
    }

    public async Task<Result> AddSocialMediaAccountAsync(int userId, SocialMediaAccountDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Platform) || string.IsNullOrWhiteSpace(dto.Username))
            return Result.Failure("Platform and username are required", "INVALID_INPUT");

        var exists = await _db.SocialMediaAccounts
            .AnyAsync(s => s.UserId == userId && s.Platform == dto.Platform, ct);
        if (exists)
            return Result.Failure($"You already have a {dto.Platform} account linked", "DUPLICATE_PLATFORM");

        _db.SocialMediaAccounts.Add(new SocialMediaAccount
        {
            UserId = userId,
            Platform = dto.Platform.Trim(),
            Username = dto.Username.Trim(),
            ProfileUrl = dto.ProfileUrl?.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return Result.Success($"{dto.Platform} account added successfully");
    }

    public async Task<Result> DeleteSocialMediaAccountAsync(int userId, int accountId, CancellationToken ct = default)
    {
        var account = await _db.SocialMediaAccounts
            .FirstOrDefaultAsync(s => s.Id == accountId && s.UserId == userId, ct);
        if (account == null)
            return Result.Failure("Account not found", "NOT_FOUND");

        _db.SocialMediaAccounts.Remove(account);
        await _db.SaveChangesAsync(ct);

        return Result.Success($"{account.Platform} account removed");
    }
}
