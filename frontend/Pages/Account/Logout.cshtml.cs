using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public LogoutModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            await _authService.LogoutAsync(userId);
        }
        HttpContext.Session.Clear();
        return RedirectToPage("/Account/Login");
    }
}
