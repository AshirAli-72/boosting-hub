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
    [BindProperty] public LoginDto LoginInput { get; set; } = new();
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string[]? Errors { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsVerified { get; set; }
    public bool IsVerificationError { get; set; }

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
            return Page();
        }

        ErrorMessage = result.Message;
        Errors = result.Errors;
        return Page();
    }

    public async Task<IActionResult> OnPostLoginAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _authService.LoginAsync(LoginInput, HttpContext);

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
