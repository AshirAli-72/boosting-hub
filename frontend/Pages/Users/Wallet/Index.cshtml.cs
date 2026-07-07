using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Users.Wallet;

public class IndexModel : PageModel
{
    private readonly IWalletService _walletService;

    public IndexModel(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public int UserId { get; set; }
    public WalletDto WalletData { get; set; } = new();

    public class WalletDto
    {
        public decimal TotalBalance { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal Withdrawn { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Active";
    }

    public async Task OnGetAsync()
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
                WalletData.Status = wallet.Status;
            }
        }
    }
}
