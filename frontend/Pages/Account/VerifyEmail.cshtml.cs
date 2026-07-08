using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Account;

public class VerifyEmailModel : PageModel
{
    public IActionResult OnGetAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Account/Register", new { error = "invalid_token" });

        return RedirectToPage("/Account/Register", new { token });
    }
}
