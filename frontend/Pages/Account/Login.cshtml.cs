using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public LoginModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [BindProperty] public LoginDto Input { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _authService.LoginAsync(Input, HttpContext);

        if (result.IsSuccess)
        {
            HttpContext.Session.SetString("AccessToken", result.Data?.AccessToken ?? "");
            HttpContext.Session.SetString("UserId", result.Data?.User?.Id.ToString() ?? "");

            var roles = result.Data?.User?.Roles ?? Array.Empty<string>();
            var email = result.Data?.User?.Email ?? "";
            var isAdmin = roles.Any(r => r.Contains("Admin")) || email == "admin@gmail.com";
            if (isAdmin)
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                return RedirectToPage("/Admin/Dashboard");
            }

            HttpContext.Session.SetString("UserRole", "User");
            return RedirectToPage("/Users/Dashboard");
        }

        ErrorMessage = result.Message;
        return Page();
    }
}
