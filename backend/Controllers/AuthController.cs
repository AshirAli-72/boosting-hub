using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using BoostingHub.backend.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var validator = new RegisterDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(Result.Failure("Validation failed", errors: validation.Errors.Select(e => e.ErrorMessage).ToArray()));

        var result = await _auth.RegisterAsync(dto, HttpContext);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var validator = new LoginDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(Result.Failure("Validation failed", errors: validation.Errors.Select(e => e.ErrorMessage).ToArray()));

        var result = await _auth.LoginAsync(dto, HttpContext);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirst("sub")?.Value!);
        var result = await _auth.LogoutAsync(userId);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var result = await _auth.RefreshTokenAsync(dto);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var validator = new ForgotPasswordDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(Result.Failure("Validation failed", errors: validation.Errors.Select(e => e.ErrorMessage).ToArray()));

        var result = await _auth.ForgotPasswordAsync(dto);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var validator = new ResetPasswordDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(Result.Failure("Validation failed", errors: validation.Errors.Select(e => e.ErrorMessage).ToArray()));

        var result = await _auth.ResetPasswordAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirst("sub")?.Value!);
        var result = await _auth.ChangePasswordAsync(userId, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        var result = await _auth.VerifyEmailAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst("sub")?.Value!);
        var result = await _auth.GetCurrentUserAsync(userId);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
