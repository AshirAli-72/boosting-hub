using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Reports;

public class UsersReportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public UsersReportModel(ApplicationDbContext db) => _db = db;

    public int TotalUsers    { get; set; }
    public int VerifiedUsers { get; set; }
    public int ActiveUsers   { get; set; }
    public int LockedUsers   { get; set; }
    public List<User> UsersTableData { get; set; } = new();
    public List<string> UserTrendLabels { get; set; } = new();
    public List<int>    UserTrendData   { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/Account/Login");

        var allRoles = await _db.Roles.ToListAsync();
        var adminRoleIds = allRoles
            .Where(r => r.RoleTitle != null && r.RoleTitle.Contains("Admin"))
            .Select(r => r.Id)
            .ToHashSet();

        var allUserRoles = await _db.UserHasRoles.ToListAsync();
        var adminUserIds = allUserRoles
            .Where(ur => adminRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .ToHashSet();

        var since = DateTime.UtcNow.Date.AddDays(-6);

        var allUsersRaw = await _db.Users.AsNoTracking().ToListAsync();
        var allUsers = allUsersRaw
            .Where(u => !adminUserIds.Contains(u.Id))
            .OrderByDescending(u => u.CreatedAt)
            .ToList();

        TotalUsers    = allUsers.Count;
        VerifiedUsers = allUsers.Count(u => u.EmailVerifiedAt != null);
        ActiveUsers   = allUsers.Count(u => u.Status == 1);
        LockedUsers   = allUsers.Count(u => u.Status == 0);
        UsersTableData = allUsers.Take(100).ToList();

        for (int i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            UserTrendLabels.Add(day.ToString("MMM dd"));
            UserTrendData.Add(allUsers.Count(u => u.CreatedAt.Date == day));
        }

        return Page();
    }
}
