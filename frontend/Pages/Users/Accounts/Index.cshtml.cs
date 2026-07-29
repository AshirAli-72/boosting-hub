using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Users.Accounts;

public class IndexModel : PageModel
{
    private readonly IAccountService _accountService;

    public IndexModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public List<AccountDto> Accounts { get; set; } = new();
    public string? SuccessMessage => TempData["Success"] as string;
    public bool HasEasyPaisaAccount { get; set; }

    [BindProperty] public CreateAccountDto Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        Input.AccountTitle = "EasyPaisa";
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            var result = await _accountService.GetAccountsByUserIdAsync(userId);
            if (result.IsSuccess && result.Data != null)
            {
                Accounts = result.Data.Where(a => a.AccountTitle == "EasyPaisa").ToList();
                HasEasyPaisaAccount = Accounts.Any();
            }
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Account/Login");

        var existing = await _accountService.GetAccountsByUserIdAsync(userId);
        if (existing.IsSuccess && existing.Data != null && existing.Data.Any(a => a.AccountTitle == "EasyPaisa"))
        {
            TempData["Error"] = "You already have an EasyPaisa account. Only one is allowed.";
            return RedirectToPage();
        }

        Input.AccountTitle = "EasyPaisa";
        var result = await _accountService.CreateAccountAsync(userId, Input);
        if (result.IsSuccess)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Account/Login");

        var result = await _accountService.DeleteAccountAsync(userId, id);
        if (result.IsSuccess)
            TempData["Success"] = result.Message;

        return RedirectToPage();
    }
}
