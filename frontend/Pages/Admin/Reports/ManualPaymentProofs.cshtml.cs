using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Reports;

public class ManualPaymentProofsReportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public ManualPaymentProofsReportModel(ApplicationDbContext db) => _db = db;

    public int TotalProofs { get; set; }
    public int PendingProofs { get; set; }
    public int PaidProofs { get; set; }
    public int RejectedProofs { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal AvgPaidAmount { get; set; }
    public List<ManualPaymentProof> ProofsTableData { get; set; } = new();
    public List<string> TrendLabels { get; set; } = new();
    public List<int> TrendData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/Account/Login");

        var since = DateTime.UtcNow.Date.AddDays(-6);

        var allProofs = await _db.ManualPaymentProofs
            .AsNoTracking()
            .Include(p => p.Order)
            .OrderByDescending(p => p.SubmitDate)
            .ToListAsync();

        TotalProofs      = allProofs.Count;
        PendingProofs    = allProofs.Count(p => p.Status == StatusHelper.ManualPaymentPending);
        PaidProofs       = allProofs.Count(p => p.Status == StatusHelper.ManualPaymentPaid);
        RejectedProofs   = allProofs.Count(p => p.Status == StatusHelper.ManualPaymentRejected);
        TotalPaidAmount  = allProofs.Where(p => p.Status == StatusHelper.ManualPaymentPaid).Sum(p => p.PaidAmount);
        AvgPaidAmount    = PaidProofs > 0 ? TotalPaidAmount / PaidProofs : 0;
        ProofsTableData  = allProofs.Take(100).ToList();

        var dailySubmissions = allProofs
            .Where(p => p.SubmitDate >= since)
            .GroupBy(p => p.SubmitDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        for (int i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            TrendLabels.Add(day.ToString("MMM dd"));
            TrendData.Add(dailySubmissions.FirstOrDefault(d => d.Date == day)?.Count ?? 0);
        }

        return Page();
    }
}
