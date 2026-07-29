using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Settings.Accounts;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<BoostingHub.backend.Models.Account> Accounts { get; set; } = new();

    [BindProperty] public BoostingHub.backend.Models.Account Input { get; set; } = new();

    public bool HasBankAccount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            Accounts = await _db.Accounts
                .Where(a => a.UserId == userId && a.AccountTitle == "Bank")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            HasBankAccount = Accounts.Any();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Account/Login");

        var existingBank = await _db.Accounts.AnyAsync(a => a.UserId == userId && a.AccountTitle == "Bank");
        if (existingBank)
        {
            TempData["Error"] = "A bank account already exists. You can only have one bank account.";
            return RedirectToPage();
        }

        Input.AccountTitle = "Bank";

        if (string.IsNullOrWhiteSpace(Input.AccountNumber))
        {
            TempData["Error"] = "Account number is required for bank accounts.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(Input.BankName))
        {
            TempData["Error"] = "Bank name is required for bank accounts.";
            return RedirectToPage();
        }

        if (Input.IsDefault)
        {
            var existingDefaults = await _db.Accounts
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();
            foreach (var d in existingDefaults)
                d.IsDefault = false;
        }

        var account = new BoostingHub.backend.Models.Account
        {
            UserId = userId,
            AccountTitle = "Bank",
            MobileNumber = Input.MobileNumber ?? "",
            Cnic = Input.Cnic ?? "",
            AccountNumber = Input.AccountNumber,
            BankName = Input.BankName,
            IsDefault = Input.IsDefault,
            Status = StatusHelper.AccountActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Bank account created successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Account/Login");

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (account != null)
        {
            _db.Accounts.Remove(account);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Account deleted.";
        }

        return RedirectToPage();
    }
}
