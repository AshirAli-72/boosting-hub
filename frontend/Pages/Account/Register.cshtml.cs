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
    public string? ErrorMessage { get; set; }
    public string[]? Errors { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _authService.RegisterAsync(Input, HttpContext);

        if (result.IsSuccess)
        {
            if (result.Data == null || result.Data.User == null)
                return RedirectToPage("/Account/Login");

            HttpContext.Session.SetString("AccessToken", result.Data.AccessToken ?? "");
            HttpContext.Session.SetString("UserId", result.Data.User.Id.ToString());
            return RedirectToPage("/User/UsersDashboard");
        }

        ErrorMessage = result.Message;
        Errors = result.Errors;
        return Page();
    }
}
