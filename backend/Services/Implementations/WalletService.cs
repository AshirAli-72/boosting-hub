using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _db;

    public WalletService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Wallet?> GetWalletByUserIdAsync(int userId)
    {
        return await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<Wallet> CreateOrUpdateWalletAsync(int userId, decimal totalBalance, string currency, decimal withdrawn, string status)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                TotalBalance = totalBalance,
                Currency = currency,
                Withdrawn = withdrawn,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
        }
        else
        {
            wallet.TotalBalance = totalBalance;
            wallet.Currency = currency;
            wallet.Withdrawn = withdrawn;
            wallet.Status = status;
        }

        await _db.SaveChangesAsync();
        return wallet;
    }

    public async Task AddRewardAsync(int userId, decimal amount)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                TotalBalance = amount,
                Currency = "USD",
                Withdrawn = 0,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
        }
        else
        {
            wallet.TotalBalance += amount;
        }

        await _db.SaveChangesAsync();
    }
}
