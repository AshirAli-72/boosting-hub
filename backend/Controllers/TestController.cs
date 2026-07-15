using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public TestController(IEmailService emailService, IConfiguration config)
    {
        _emailService = emailService;
        _config = config;
    }

    [HttpPost("smtp")]
    public async Task<IActionResult> TestSmtp([FromQuery] string to)
    {
        if (string.IsNullOrWhiteSpace(to))
            to = _config["Email:SmtpUser"] ?? "";

        var host = _config["Email:SmtpHost"] ?? "NOT SET";
        var port = _config["Email:SmtpPort"] ?? "NOT SET";
        var user = _config["Email:SmtpUser"] ?? "NOT SET";
        var pass = (_config["Email:SmtpPass"] ?? "").Replace(" ", "");

        try
        {
            await _emailService.SendEmailAsync(to, "Boosting Hub - SMTP Test", "<h1>It works!</h1><p>Your SMTP is configured correctly.</p>");
            return Ok(new { success = true, message = $"Email sent to {to}" });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message;
            return Ok(new { success = false, message = ex.Message, inner, host, port, user, passLength = pass.Length, passFirst4 = pass.Length >= 4 ? pass[..4] : "" });
        }
    }
}
