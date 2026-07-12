using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Reports;

public class TasksReportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public TasksReportModel(ApplicationDbContext db) => _db = db;

    public int TotalTasks     { get; set; }
    public int ActiveTasks    { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingProofs  { get; set; }
    public int ApprovedProofs { get; set; }
    public int RejectedProofs { get; set; }
    public List<TaskGenerate> TasksTableData { get; set; } = new();
    public List<string> TaskTrendLabels { get; set; } = new();
    public List<int>    TaskTrendData   { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/Account/Login");

        var since = DateTime.UtcNow.Date.AddDays(-6);

        TotalTasks     = await _db.TaskGenerates.CountAsync();
        ActiveTasks    = await _db.TaskGenerates.CountAsync(t => t.Status == "Active");
        CompletedTasks = await _db.TaskCompletes.CountAsync(tc => tc.Status == "Completed");
        PendingProofs  = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == "Pending");
        ApprovedProofs = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == "Approved");
        RejectedProofs = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == "Rejected");
        TasksTableData = await _db.TaskGenerates.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt).Take(100).ToListAsync();

        var dailyTasks = await _db.TaskCompletes
            .Where(tc => tc.Status == "Completed" && tc.Date >= since)
            .GroupBy(tc => tc.Date.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        for (int i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            TaskTrendLabels.Add(day.ToString("MMM dd"));
            TaskTrendData.Add(dailyTasks.FirstOrDefault(d => d.Date == day)?.Count ?? 0);
        }

        return Page();
    }
}
