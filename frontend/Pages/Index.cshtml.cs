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
    public int CompletedTasks { get; set; }
    public List<User> RecentUsers { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var allRoles = await _db.Roles.ToListAsync();
            var adminRoleIds = allRoles
                .Where(r => r.RoleTitle.Contains("Admin"))
                .Select(r => r.Id)
                .ToHashSet();

            var allUserRoles = await _db.UserHasRoles.ToListAsync();
            var adminUserIds = allUserRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .ToHashSet();

            const string seederEmail = "admin@gmail.com";

            var allUsers = await _db.Users.ToListAsync();
            var filteredUsers = allUsers
                .Where(u => !adminUserIds.Contains(u.Id) && u.Email != seederEmail && u.EmailVerifiedAt != null)
                .ToList();

            TotalUsers = filteredUsers.Count;

            TotalOrders = await _db.Orders.CountAsync();

            var tasks = await _db.TaskGenerates.ToListAsync();
            var completedCounts = await _db.TaskCompletes
                .Where(tc => tc.Status == "Completed")
                .GroupBy(tc => tc.TaskId)
                .Select(g => new { TaskId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TaskId, x => x.Count);

            CompletedTasks = tasks.Count(t => completedCounts.GetValueOrDefault(t.Id, 0) >= t.Quantity && t.Quantity > 0);

            RecentUsers = filteredUsers
                .OrderByDescending(u => u.CreatedAt)
                .Take(8)
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt
                })
                .ToList();
        }
        catch
        {
            TotalUsers = 0;
            TotalOrders = 0;
            CompletedTasks = 0;
            RecentUsers = new List<User>();
        }

        ViewData["LandingTotalUsers"] = TotalUsers;
        ViewData["LandingTotalOrders"] = TotalOrders;
        ViewData["LandingCompletedTasks"] = CompletedTasks;
        ViewData["RecentUsers"] = RecentUsers;
    }
}
