using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class VerifyEmailChangeModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public VerifyEmailChangeModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public bool Verified { get; set; }
    public bool Error { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            Error = true;
            ErrorMessage = "Invalid verification link";
            return Page();
        }

        var result = await _authService.VerifyEmailChangeAsync(email, token);

        if (result.IsSuccess)
        {
            Verified = true;
            SuccessMessage = result.Message;
        }
        else
        {
            Error = true;
            ErrorMessage = result.Message;
        }

        return Page();
    }
}
