using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class SecuritySettingsModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public SecuritySettingsModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [BindProperty] public ChangePasswordDto ChangePassword { get; set; } = new();
    public string CurrentUserName { get; set; } = "User";
    public string? SuccessMessage => TempData["Success"] as string;

    public async Task OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            var result = await _authService.GetCurrentUserAsync(userId);
            if (result.IsSuccess)
                CurrentUserName = result.Data?.Name ?? "User";
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        if (!ModelState.IsValid) return Page();
        var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        if (userId == 0) return RedirectToPage("/Account/Login");

        var result = await _authService.ChangePasswordAsync(userId, ChangePassword);
        if (result.IsSuccess)
            TempData["Success"] = "Password changed successfully";
        else
            ModelState.AddModelError("", result.Message ?? "");

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var uid))
        {
            var userResult = await _authService.GetCurrentUserAsync(uid);
            if (userResult.IsSuccess)
                CurrentUserName = userResult.Data?.Name ?? "User";
        }
        return Page();
    }
}
