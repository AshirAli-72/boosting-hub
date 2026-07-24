using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Users.Wallet;

public class IndexModel : PageModel
{
    private readonly IWalletService _walletService;
    private readonly IAccountService _accountService;

    public IndexModel(IWalletService walletService, IAccountService accountService)
    {
        _walletService = walletService;
        _accountService = accountService;
    }

    public int UserId { get; set; }
    public WalletDto WalletData { get; set; } = new();
    public AccountDto? DefaultAccount { get; set; }

    public class WalletDto
    {
        public decimal TotalBalance { get; set; }
        public string Currency { get; set; } = "PKR";
        public decimal Withdrawn { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Active";
    }

    [BindProperty] public decimal WithdrawAmount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        UserId = int.TryParse(userIdStr, out var id) ? id : 0;

        if (UserId > 0)
        {
            var wallet = await _walletService.GetWalletByUserIdAsync(UserId);
            if (wallet != null)
            {
                WalletData.TotalBalance = wallet.TotalBalance;
                WalletData.Currency = wallet.Currency;
                WalletData.Withdrawn = wallet.Withdrawn;
                WalletData.CreatedAt = wallet.CreatedAt;
                WalletData.Status = StatusHelper.WalletStatusToString(wallet.Status);
            }

            var accountsResult = await _accountService.GetAccountsByUserIdAsync(UserId);
            if (accountsResult.IsSuccess && accountsResult.Data != null)
                DefaultAccount = accountsResult.Data.FirstOrDefault(a => a.IsDefault && a.Status == StatusHelper.AccountStatusToString(StatusHelper.AccountActive))
                    ?? accountsResult.Data.FirstOrDefault(a => a.Status == StatusHelper.AccountStatusToString(StatusHelper.AccountActive));
        }
        return Page();
    }

    public async Task<JsonResult> OnPostChangeCurrencyAsync(string currency)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return new JsonResult(new { success = false, message = "Not logged in" });

        var valid = new[] { "USD", "EUR", "GBP", "PKR", "INR", "BDT", "SAR", "AED", "TRY", "NGN", "JPY", "CNY", "KRW", "PHP", "IDR", "MYR", "THB", "EGP", "ZAR", "MXN" };
        if (!valid.Contains(currency))
            return new JsonResult(new { success = false, message = "Invalid currency" });

        var ok = await _walletService.UpdateCurrencyAsync(userId, currency);
        if (!ok)
            return new JsonResult(new { success = false, message = "Wallet not found" });

        var wallet = await _walletService.GetWalletByUserIdAsync(userId);
        return new JsonResult(new
        {
            success = true,
            balance = wallet?.TotalBalance ?? 0m,
            withdrawn = wallet?.Withdrawn ?? 0m,
            currency = wallet?.Currency ?? currency
        });
    }

    public async Task<IActionResult> OnPostWithdrawAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Account/Login");

        if (WithdrawAmount <= 0)
        {
            TempData["Error"] = "Invalid withdrawal amount";
            return RedirectToPage();
        }

        var wallet = await _walletService.GetWalletByUserIdAsync(userId);
        if (wallet == null || wallet.TotalBalance < WithdrawAmount)
        {
            TempData["Error"] = "Insufficient balance";
            return RedirectToPage();
        }

        var accountsResult = await _accountService.GetAccountsByUserIdAsync(userId);
        var defaultAccount = accountsResult.IsSuccess && accountsResult.Data != null
            ? accountsResult.Data.FirstOrDefault(a => a.IsDefault && a.Status == StatusHelper.AccountStatusToString(StatusHelper.AccountActive))
                ?? accountsResult.Data.FirstOrDefault(a => a.Status == StatusHelper.AccountStatusToString(StatusHelper.AccountActive))
            : null;

        if (defaultAccount == null)
        {
            TempData["Error"] = "No active account found. Please add an account first.";
            return RedirectToPage();
        }

        var walletCurrency = wallet?.Currency ?? "PKR";
        var sym = walletCurrency switch { "PKR" => "₨", "EUR" => "€", "GBP" => "£", "INR" => "₹", "BDT" => "৳", _ => "$" };
        await _walletService.WithdrawAsync(userId, WithdrawAmount);
        TempData["Success"] = $"Withdrawal of {sym}{WithdrawAmount:N2} ({walletCurrency}) requested to {defaultAccount.AccountTitle} ({defaultAccount.MobileNumber})";

        return RedirectToPage();
    }
}
