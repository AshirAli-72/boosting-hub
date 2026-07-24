using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin.Settings.Packages;

public class IndexModel : PageModel
{
    public string UserRole { get; set; } = "";

    public void OnGet()
    {
        UserRole = HttpContext.Session.GetString("UserRole") ?? "";
    }
}
