using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Reports;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    // ── Revenue / Orders ──────────────────────────────────────────────────────
    public decimal TotalRevenue     { get; set; }
    public decimal AvgOrderValue    { get; set; }
    public int     TotalOrders      { get; set; }
    public int     OrdersCompleted  { get; set; }
    public int     OrdersPending    { get; set; }
    public int     OrdersInProgress { get; set; }
    public List<Orders> RevenueTableData { get; set; } = new();

    // ── Users ─────────────────────────────────────────────────────────────────
    public int TotalUsers    { get; set; }
    public int VerifiedUsers { get; set; }
    public int ActiveUsers   { get; set; }
    public int LockedUsers   { get; set; }
    public List<User> UsersTableData { get; set; } = new();

    // ── Tasks ─────────────────────────────────────────────────────────────────
    public int TotalTasks     { get; set; }
    public int ActiveTasks    { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingProofs  { get; set; }
    public int ApprovedProofs { get; set; }
    public int RejectedProofs { get; set; }
    public List<TaskGenerate> TasksTableData { get; set; } = new();

    // ── Orders table (for orders hub) ─────────────────────────────────────────
    public List<Orders> OrdersTableData { get; set; } = new();

    // ── Trend data (last 7 days) ──────────────────────────────────────────────
    public List<string> OrderTrendLabels { get; set; } = new();
    public List<int>    OrderTrendData   { get; set; } = new();
    public List<string> UserTrendLabels  { get; set; } = new();
    public List<int>    UserTrendData    { get; set; } = new();
    public List<string> TaskTrendLabels  { get; set; } = new();
    public List<int>    TaskTrendData    { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/Account/Login");

        var since = DateTime.UtcNow.Date.AddDays(-6);

        // ── Revenue / Orders ──────────────────────────────────────────────────
        var allOrders = await _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();
        TotalOrders     = allOrders.Count;
        TotalRevenue    = allOrders.Where(o => o.Status == StatusHelper.OrderApproved).Sum(o => o.Budget);
        AvgOrderValue   = TotalOrders > 0 ? allOrders.Sum(o => o.Budget) / TotalOrders : 0;
        OrdersCompleted = allOrders.Count(o => o.Status == StatusHelper.OrderApproved);
        OrdersPending   = allOrders.Count(o => o.Status == StatusHelper.OrderPending);
        OrdersInProgress= allOrders.Count(o => o.Status == StatusHelper.OrderPending);
        RevenueTableData = allOrders.Where(o => o.Status == StatusHelper.OrderApproved).Take(50).ToList();
        OrdersTableData  = allOrders.Take(50).ToList();

        var dailyOrders = allOrders
            .Where(o => o.CreatedAt >= since)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        for (int i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            OrderTrendLabels.Add(day.ToString("MMM dd"));
            OrderTrendData.Add(dailyOrders.FirstOrDefault(d => d.Date == day)?.Count ?? 0);
        }

        // ── Users ─────────────────────────────────────────────────────────────
        var allUsers = await _db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).ToListAsync();
        TotalUsers    = allUsers.Count;
        VerifiedUsers = allUsers.Count(u => u.EmailVerifiedAt != null);
        ActiveUsers   = allUsers.Count(u => u.Status == 1);
        LockedUsers   = allUsers.Count(u => u.Status == 0);
        UsersTableData = allUsers.Take(50).ToList();

        for (int i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            UserTrendLabels.Add(day.ToString("MMM dd"));
            UserTrendData.Add(allUsers.Count(u => u.CreatedAt.Date == day));
        }

        // ── Tasks ─────────────────────────────────────────────────────────────
        TotalTasks     = await _db.TaskGenerates.CountAsync();
        ActiveTasks    = await _db.TaskGenerates.CountAsync(t => t.Status == StatusHelper.TaskGenerateActive);
        CompletedTasks = await _db.TaskCompletes.CountAsync(tc => tc.Status == StatusHelper.TaskCompleteCompleted);
        PendingProofs  = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == StatusHelper.VerificationPendingReview);
        ApprovedProofs = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == StatusHelper.VerificationApproved);
        RejectedProofs = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == StatusHelper.VerificationRejected);
        TasksTableData = await _db.TaskGenerates.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync();

        var dailyTasks = await _db.TaskCompletes
            .Where(tc => tc.Status == StatusHelper.TaskCompleteCompleted && tc.Date >= since)
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
