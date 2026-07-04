using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public ResetPasswordModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [BindProperty] public ResetPasswordDto Input { get; set; } = new();
    public bool Success { get; set; }

    public void OnGet(string? email, string? token)
    {
        Input.Email = email ?? "";
        Input.Token = token ?? "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var result = await _authService.ResetPasswordAsync(Input);
        if (result.IsSuccess)
            Success = true;
        else
            ModelState.AddModelError("", result.Message ?? "");
        return Page();
    }
}
