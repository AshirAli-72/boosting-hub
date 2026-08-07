using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/email-update")]
public class EmailUpdateController : ControllerBase
{
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailUpdateController> _logger;

    public EmailUpdateController(
        ApplicationDbContext db,
        IEmailService emailService,
        ILogger<EmailUpdateController> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var raw = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(raw, out var id) && id > 0) return id;
        var sessionId = HttpContext.Session.GetString("UserId");
        return int.TryParse(sessionId, out var sid) ? sid : 0;
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendLinkRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewEmail) || !req.NewEmail.Contains('@'))
            return BadRequest(new { success = false, message = "Please enter a valid email address." });

        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new { success = false, message = "Not authenticated." });

        var newEmail = req.NewEmail.Trim().ToLower();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != userId);
        if (emailTaken)
            return BadRequest(new { success = false, message = "This email is already in use by another account." });

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized(new { success = false, message = "Not authenticated." });

        var otp = GenerateOtp();

        // Invalidate any previous pending OTPs for this user
        var pending = await _db.EmailChanges
            .Where(x => x.UserId == userId && !x.IsUsed)
            .ToListAsync();
        foreach (var p in pending)
            p.IsUsed = true;

        _db.EmailChanges.Add(new EmailChange
        {
            UserId = userId,
            OldEmail = user.Email,
            NewEmail = newEmail,
            OtpCode = HashOtp(otp),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var userName = user.Name ?? "User";
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
                      <h2 style="font-size:18px;font-weight:700;color:#1E293B;margin:0 0 16px;">Confirm Your New Email</h2>
                      <p style="font-size:15px;color:#1E293B;line-height:1.7;margin:0 0 24px;">Use the code below to confirm changing your email to <strong>{newEmail}</strong>.</p>
                      <div style="text-align:center;margin:0 0 24px;">
                        <span style="display:inline-block;padding:16px 40px;font-size:28px;font-weight:800;letter-spacing:8px;color:#1E293B;background:#F1F5F9;border:2px dashed #7C3AED;border-radius:12px;">{otp}</span>
                      </div>
                      <p style="font-size:13px;color:#64748B;text-align:center;margin:0 0 8px;">This code expires in <strong>10 minutes</strong>.</p>
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

        try
        {
            await _emailService.SendEmailAsync(newEmail, "Boosting Hub – Confirm Your New Email", emailHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change OTP to {Email} for user {UserId}", newEmail, userId);
            return StatusCode(500, new { success = false, message = "Failed to send verification code. Please try again." });
        }

        _logger.LogInformation("Email change OTP sent to {Email} for user {UserId}", newEmail, userId);

        return Ok(new { success = true, message = $"A verification code has been sent to {newEmail}. It expires in 10 minutes." });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewEmail) || string.IsNullOrWhiteSpace(req.OtpCode))
            return BadRequest(new { success = false, message = "Please enter the verification code." });

        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new { success = false, message = "Not authenticated." });

        var newEmail = req.NewEmail.Trim().ToLower();
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized(new { success = false, message = "Not authenticated." });

        var record = await _db.EmailChanges
            .Where(x => x.UserId == userId && x.NewEmail == newEmail && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (record == null)
            return BadRequest(new { success = false, message = "No pending verification found. Please request a new code." });

        if (DateTime.UtcNow - record.CreatedAt > OtpExpiry)
            return BadRequest(new { success = false, message = "This code has expired. Please request a new one." });

        if (record.OtpCode != HashOtp(req.OtpCode.Trim()))
            return BadRequest(new { success = false, message = "Invalid verification code. Please try again." });

        var alreadyTaken = await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != userId);
        if (alreadyTaken)
            return BadRequest(new { success = false, message = "This email was taken by someone else. Please request a new code." });

        var oldEmail = user.Email;
        user.Email = newEmail;
        record.IsUsed = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} verified OTP and updated email from {OldEmail} to {NewEmail}", userId, oldEmail, newEmail);

        return Ok(new { success = true, message = "Email updated successfully." });
    }

    private static string GenerateOtp()
    {
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return (BitConverter.ToUInt32(bytes, 0) % 1_000_000).ToString("D6");
    }

    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToBase64String(bytes);
    }
}

public class SendLinkRequest
{
    public string NewEmail { get; set; } = "";
}

public class VerifyOtpRequest
{
    public string NewEmail { get; set; } = "";
    public string OtpCode { get; set; } = "";
}
