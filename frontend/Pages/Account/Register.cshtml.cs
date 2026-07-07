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

    public void OnGet() { }

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
}
