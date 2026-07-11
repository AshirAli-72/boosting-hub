using BoostingHub.backend.Data;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/email-update")]
public class EmailUpdateController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailUpdateController> _logger;

    public EmailUpdateController(ApplicationDbContext db, IMemoryCache cache, IEmailService emailService, ILogger<EmailUpdateController> logger)
    {
        _db = db;
        _cache = cache;
        _emailService = emailService;
        _logger = logger;
    }

    private int GetUserId()
    {
        // Try JWT claims first, fall back to session
        var raw = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(raw, out var id) && id > 0) return id;
        var sessionId = HttpContext.Session.GetString("UserId");
        return int.TryParse(sessionId, out var sid) ? sid : 0;
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewEmail) || !req.NewEmail.Contains('@'))
            return BadRequest(new { success = false, message = "Please enter a valid email address." });

        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new { success = false, message = "Not authenticated." });

        // Check email isn't already taken
        var emailTaken = await _db.Users.AnyAsync(u => u.Email == req.NewEmail.Trim().ToLower() && u.Id != userId);
        if (emailTaken)
            return BadRequest(new { success = false, message = "This email is already in use by another account." });

        // Generate a cryptographically secure 6-digit OTP
        var otp = GenerateSecureOtp();
        var cacheKey = $"email_otp_{userId}";
        var otpData = new OtpCacheEntry { Otp = otp, NewEmail = req.NewEmail.Trim().ToLower(), Attempts = 0 };
        _cache.Set(cacheKey, otpData, TimeSpan.FromMinutes(5));

        // Send OTP email
        try
        {
            var user = await _db.Users.FindAsync(userId);
            var userName = user?.Name ?? "User";

            var emailHtml = $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"></head>
                <body style="margin:0;padding:0;background:#F8FAFC;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;">
                  <table role="presentation" style="width:100%;border-collapse:collapse;">
                    <tr><td style="padding:40px 16px;">
                      <table role="presentation" style="max-width:520px;margin:0 auto;background:#fff;border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,0.08);border-collapse:collapse;overflow:hidden;">
                        <tr>
                          <td style="background:linear-gradient(135deg,#7C3AED,#0D9488);padding:28px 40px;text-align:center;">
                            <h1 style="margin:0;font-size:20px;font-weight:700;color:#fff;">Boosting Hub</h1>
                          </td>
                        </tr>
                        <tr><td style="padding:36px 40px 28px;">
                          <p style="font-size:15px;color:#64748B;margin:0 0 6px;">Hello {userName},</p>
                          <h2 style="font-size:18px;font-weight:700;color:#1E293B;margin:0 0 16px;">Email Change Verification</h2>
                          <p style="font-size:15px;color:#1E293B;line-height:1.7;margin:0 0 24px;">Your one-time verification code for changing your email address is:</p>
                          <div style="text-align:center;margin:0 0 24px;">
                            <span style="display:inline-block;padding:14px 36px;font-size:2rem;font-weight:800;letter-spacing:10px;background:#F1F5F9;border-radius:12px;color:#7C3AED;border:2px dashed #7C3AED;font-family:monospace;">{otp}</span>
                          </div>
                          <p style="font-size:13px;color:#64748B;text-align:center;margin:0 0 8px;">This code expires in <strong>5 minutes</strong>. Do not share it with anyone.</p>
                          <p style="font-size:13px;color:#94A3B8;text-align:center;margin:0;">If you did not request this, you can safely ignore this email.</p>
                        </td></tr>
                        <tr>
                          <td style="padding:18px 40px;background:#F8FAFC;border-top:1px solid #E2E8F0;text-align:center;">
                            <p style="margin:0;font-size:12px;color:#94A3B8;">&copy; 2026 Boosting Hub. All rights reserved.</p>
                          </td>
                        </tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;

            await _emailService.SendEmailAsync(
                req.NewEmail.Trim(),
                "Boosting Hub – Email Change OTP",
                emailHtml
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send OTP email to {Email}", req.NewEmail);
        }

        _logger.LogInformation("Email change OTP sent to {Email} for user {UserId}", req.NewEmail, userId);

        return Ok(new { success = true, message = $"A 6-digit verification code has been sent to {req.NewEmail}. Check your inbox." });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Otp) || req.Otp.Length < 6)
            return BadRequest(new { success = false, message = "Please enter the 6-digit OTP." });

        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new { success = false, message = "Not authenticated." });

        var cacheKey = $"email_otp_{userId}";
        if (!_cache.TryGetValue(cacheKey, out OtpCacheEntry? cached) || cached == null)
            return BadRequest(new { success = false, message = "OTP expired or not found. Please request a new one." });

        // Increment attempt count — brute-force protection
        cached.Attempts++;
        if (cached.Attempts > 5)
        {
            _cache.Remove(cacheKey);
            return BadRequest(new { success = false, message = "Too many failed attempts. Please request a new OTP." });
        }

        if (cached.Otp != req.Otp.Trim())
            return BadRequest(new { success = false, message = $"Invalid OTP. {6 - cached.Attempts} attempt(s) remaining." });

        // OTP is valid — update email
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { success = false, message = "User not found." });

        // Double-check for race conditions
        var alreadyTaken = await _db.Users.AnyAsync(u => u.Email == cached.NewEmail && u.Id != userId);
        if (alreadyTaken) return BadRequest(new { success = false, message = "This email was taken by someone else. Please choose a different one." });

        user.Email = cached.NewEmail;
        await _db.SaveChangesAsync();
        _cache.Remove(cacheKey);

        _logger.LogInformation("User {UserId} successfully updated email to {Email}", userId, cached.NewEmail);

        return Ok(new { success = true, message = "Email updated successfully! Please log in again with your new email." });
    }

    private static string GenerateSecureOtp()
    {
        // Generate a 6-digit OTP using cryptographically strong random bytes
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1_000_000;
        return number.ToString("D6"); // always 6 digits
    }
}

public class SendOtpRequest
{
    public string NewEmail { get; set; } = "";
}

public class VerifyOtpRequest
{
    public string Otp { get; set; } = "";
}

public class OtpCacheEntry
{
    public string Otp { get; set; } = "";
    public string NewEmail { get; set; } = "";
    public int Attempts { get; set; }
}
