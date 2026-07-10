using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin.ProofReview;

public class IndexModel : PageModel
{
    public int AdminUserId { get; set; }

    public void OnGet()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        AdminUserId = int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
