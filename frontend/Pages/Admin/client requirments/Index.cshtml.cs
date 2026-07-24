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

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var packages = await _db.Packages.ToDictionaryAsync(p => p.Id);

        Orders = await _db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        foreach (var order in Orders)
        {
            if (order.PackageId.HasValue && packages.TryGetValue(order.PackageId.Value, out var pkg))
            {
                order.Budget = pkg.Price;
            }
        }

        return Page();
    }
}
