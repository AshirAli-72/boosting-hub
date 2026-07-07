using BoostingHub.backend.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Users.Tasks;

public class AvailableModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public AvailableModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        UserId = int.TryParse(userIdStr, out var id) ? id : 0;

        if (UserId > 0)
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == UserId);
            UserName = user?.Name ?? "User";
        }
        else
        {
            UserName = "User";
        }
    }
}
