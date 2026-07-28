using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Inquiries;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<Orders> Orders { get; set; } = new();
    public Dictionary<int, ManualPaymentProof> PaymentProofs { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        Orders = await _db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        foreach (var order in Orders)
        {
            order.Budget = order.TotalAmount;
        }

        var orderIds = Orders.Select(o => o.Id).ToList();
        var proofs = await _db.ManualPaymentProofs
            .Where(p => orderIds.Contains(p.OrderId))
            .GroupBy(p => p.OrderId)
            .Select(g => g.OrderByDescending(p => p.SubmitDate).First())
            .ToListAsync();

        foreach (var proof in proofs)
        {
            PaymentProofs[proof.OrderId] = proof;
        }

        return Page();
    }
}
