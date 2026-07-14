using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
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
    public List<User> RecentUsers { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var adminRoleIds = await _db.Roles
                .Where(r => r.RoleTitle.Contains("Admin"))
                .Select(r => r.Id)
                .ToListAsync();

            var adminUserIds = await _db.UserHasRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            var seederEmails = new[] { "admin@gmail.com" };

            TotalUsers = await _db.Users
                .Where(u => !adminUserIds.Contains(u.Id) && !seederEmails.Contains(u.Email!) && u.EmailVerifiedAt != null)
                .CountAsync();

            TotalOrders = await _db.Orders.CountAsync();
            TotalRevenue = await _db.Orders
                .Where(o => o.Status == "Approved")
                .SumAsync(o => o.Budget);

            RecentUsers = await _db.Users
                .Where(u => !adminUserIds.Contains(u.Id) && !seederEmails.Contains(u.Email!) && u.EmailVerifiedAt != null)
                .OrderByDescending(u => u.CreatedAt)
                .Take(8)
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
        }
        catch
        {
            TotalUsers = 0;
            TotalOrders = 0;
            TotalRevenue = 0;
            RecentUsers = new List<User>();
        }

        ViewData["LandingTotalUsers"] = TotalUsers;
        ViewData["LandingTotalOrders"] = TotalOrders;
        ViewData["LandingTotalRevenue"] = TotalRevenue;
        ViewData["RecentUsers"] = RecentUsers;
    }
}
