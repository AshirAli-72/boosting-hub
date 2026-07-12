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

        var adminUserIds = await _db.UserHasRoles
            .Where(ur => ur.Role!.RoleTitle.Contains("Admin"))
            .Select(ur => ur.UserId)
            .ToListAsync();

        var since = DateTime.UtcNow.Date.AddDays(-6);

        var allUsers = await _db.Users
            .Where(u => !adminUserIds.Contains(u.Id))
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

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
