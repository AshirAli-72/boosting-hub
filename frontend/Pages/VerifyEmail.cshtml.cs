using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages;

public class VerifyEmailModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public VerifyEmailModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsVerified { get; set; }
    public bool IsVerificationError { get; set; }

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return LocalRedirect("/register");

        try
        {
            var result = await _authService.VerifyEmailAsync(new VerifyEmailDto { Token = token });

            if (result.IsSuccess)
            {
                IsVerified = true;
                Message = "Your email has been verified. You can now log in.";
                return Page();
            }

            ErrorMessage = result.Message;
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred during verification. Please try again.";
            return Page();
        }
    }
}
