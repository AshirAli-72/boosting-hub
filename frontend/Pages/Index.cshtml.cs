using BoostingHub.backend.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    public int TotalUsers { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }

    public async Task OnGetAsync()
    {
        var adminUserIds = await _db.UserHasRoles
            .Where(ur => ur.Role!.RoleTitle.Contains("Admin"))
            .Select(ur => ur.UserId)
            .ToListAsync();

        TotalUsers = await _db.Users.CountAsync(u => !adminUserIds.Contains(u.Id));
        TotalOrders = await _db.Orders.CountAsync();
        TotalRevenue = await _db.Orders
            .Where(o => o.Status == "Approved")
            .SumAsync(o => o.Budget);

        ViewData["LandingTotalUsers"] = TotalUsers;
        ViewData["LandingTotalOrders"] = TotalOrders;
        ViewData["LandingTotalRevenue"] = TotalRevenue;
    }
}
