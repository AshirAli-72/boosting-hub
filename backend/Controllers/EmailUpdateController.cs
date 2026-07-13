using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BoostingHub.backend.Data;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/email-update")]
public class EmailUpdateController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailUpdateController> _logger;

    public EmailUpdateController(
        ApplicationDbContext db,
        IEmailService emailService,
        IConfiguration config,
        ILogger<EmailUpdateController> logger)
    {
        _db = db;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    private int GetUserId()
    {
        var raw = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(raw, out var id) && id > 0) return id;
        var sessionId = HttpContext.Session.GetString("UserId");
        return int.TryParse(sessionId, out var sid) ? sid : 0;
    }

    [HttpPost("send-link")]
    public async Task<IActionResult> SendLink([FromBody] SendLinkRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewEmail) || !req.NewEmail.Contains('@'))
            return BadRequest(new { success = false, message = "Please enter a valid email address." });

        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new { success = false, message = "Not authenticated." });

        var emailLower = req.NewEmail.Trim().ToLower();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == emailLower && u.Id != userId);
        if (emailTaken)
            return BadRequest(new { success = false, message = "This email is already in use by another account." });

        var token = EncodeEmailChangeToken(userId, emailLower);
        var verificationLink = $"{Request.Scheme}://{Request.Host}/api/email-update/verify?token={Uri.EscapeDataString(token)}";

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
                      <h2 style="font-size:18px;font-weight:700;color:#1E293B;margin:0 0 16px;">Verify Your New Email</h2>
                      <p style="font-size:15px;color:#1E293B;line-height:1.7;margin:0 0 24px;">Click the button below to confirm changing your email to <strong>{emailLower}</strong>.</p>
                      <div style="text-align:center;margin:0 0 24px;">
                        <a href="{verificationLink}" target="_self" style="display:inline-block;padding:14px 36px;font-size:16px;font-weight:600;color:#fff;background:linear-gradient(135deg,#7C3AED,#0D9488);border-radius:10px;text-decoration:none;">Verify Email Address</a>
                      </div>
                      <p style="font-size:13px;color:#64748B;text-align:center;margin:0 0 8px;">This link expires in <strong>24 hours</strong>.</p>
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
            await _emailService.SendEmailAsync(emailLower, "Boosting Hub – Verify Your New Email", emailHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change verification link to {Email} for user {UserId}", emailLower, userId);
            return StatusCode(500, new { success = false, message = "Failed to send verification email. Please try again." });
        }

        _logger.LogInformation("Email change verification link sent to {Email} for user {UserId}", emailLower, userId);

        return Ok(new { success = true, message = $"A verification link has been sent to {emailLower}. Check your inbox and click the link to confirm." });
    }

    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Invalid verification link.");

        var payload = DecodeEmailChangeToken(token);
        if (payload == null)
            return BadRequest("Invalid or expired verification link. Please request a new one.");

        var user = await _db.Users.FindAsync(payload.UserId);
        if (user == null)
            return BadRequest("User not found.");

        var alreadyTaken = await _db.Users.AnyAsync(u => u.Email == payload.NewEmail && u.Id != payload.UserId);
        if (alreadyTaken)
            return BadRequest("This email was taken by someone else. Please request a new verification link.");

        user.Email = payload.NewEmail;
        user.EmailChangeToken = token;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} successfully verified and updated email to {Email}", payload.UserId, payload.NewEmail);

        return Redirect($"/settings?email_verified=1");
    }

    private string EncodeEmailChangeToken(int userId, string newEmail)
    {
        var payload = JsonSerializer.Serialize(new
        {
            UserId = userId,
            NewEmail = newEmail,
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

    private EmailChangePayload? DecodeEmailChangeToken(string token)
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
            var data = JsonSerializer.Deserialize<EmailChangePayload>(Encoding.UTF8.GetString(plaintext));
            if (data == null) return null;
            if (DateTime.UtcNow - data.CreatedAt > TimeSpan.FromHours(24)) return null;
            return data;
        }
        catch
        {
            return null;
        }
    }

    private record EmailChangePayload
    {
        public int UserId { get; init; }
        public string NewEmail { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }
}

public class SendLinkRequest
{
    public string NewEmail { get; set; } = "";
}
