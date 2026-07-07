using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class VerifyEmailModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public VerifyEmailModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public bool Success { get; set; }
    public bool Error { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Error = true;
            ErrorMessage = "Invalid verification link";
            return Page();
        }

        var result = await _authService.VerifyEmailAsync(new VerifyEmailDto { Token = token });

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Your email has been verified successfully. You can now log in.";
            return RedirectToPage("/Account/Login");
        }

        Error = true;
        ErrorMessage = result.Message;
        return Page();
    }
}
