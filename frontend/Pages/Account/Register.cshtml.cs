using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public RegisterModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [BindProperty] public RegisterDto Input { get; set; } = new();
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string[]? Errors { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsVerified { get; set; }
    public bool IsVerificationError { get; set; }
    public string? VerificationLink { get; set; }

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            var result = await _authService.VerifyEmailAsync(new VerifyEmailDto { Token = token });

            if (result.IsSuccess)
            {
                IsVerified = true;
                Message = "Your email has been verified. You can now log in.";
                return Page();
            }

            IsVerificationError = true;
            ErrorMessage = result.Message;
            return Page();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _authService.RegisterAsync(Input, HttpContext);

        if (result.IsSuccess)
        {
            IsSuccess = true;
            Message = result.Message;
            VerificationLink = result.Data?.VerificationLink;
            return Page();
        }

        ErrorMessage = result.Message;
        Errors = result.Errors;
        return Page();
    }
}
