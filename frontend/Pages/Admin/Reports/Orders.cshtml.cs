using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Reports;

public class OrdersReportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public OrdersReportModel(ApplicationDbContext db) => _db = db;

    public int     TotalOrders      { get; set; }
    public int     OrdersCompleted  { get; set; }
    public int     OrdersPending    { get; set; }
    public int     OrdersInProgress { get; set; }
    public List<Orders> OrdersTableData { get; set; } = new();
    public List<string> OrderTrendLabels { get; set; } = new();
    public List<int>    OrderTrendData   { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/Account/Login");

        var since = DateTime.UtcNow.Date.AddDays(-6);

        var allOrders = await _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();
        TotalOrders      = allOrders.Count;
        OrdersCompleted  = allOrders.Count(o => o.Status == "Approved");
        OrdersPending    = allOrders.Count(o => o.Status == "Pending");
        OrdersInProgress = allOrders.Count(o => o.Status == "in_progress");
        OrdersTableData  = allOrders.Take(100).ToList();

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

        return Page();
    }
}
