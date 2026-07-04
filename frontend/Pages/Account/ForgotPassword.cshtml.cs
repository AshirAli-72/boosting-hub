using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public ForgotPasswordModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [BindProperty] public ForgotPasswordDto Input { get; set; } = new();
    public bool Success { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var result = await _authService.ForgotPasswordAsync(Input);
        Success = true;
        return Page();
    }
}
