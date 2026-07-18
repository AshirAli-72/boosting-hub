using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<WalletService> _logger;

    public WalletService(ApplicationDbContext db, ILogger<WalletService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Wallet?> GetWalletByUserIdAsync(int userId)
    {
        return await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<Wallet> CreateOrUpdateWalletAsync(int userId, decimal totalBalance, string currency, decimal withdrawn, int status)
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
                Status = StatusHelper.WalletActive,
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

    public async Task CreditRewardAsync(int userId, decimal amount, int taskId, int proofId, string taskCurrency = "USD")
    {
        var wallet = await GetOrCreateWalletAsync(userId);

        var convertedAmount = ConvertCurrency(amount, taskCurrency, wallet.Currency);
        var balanceBefore = wallet.TotalBalance;
        wallet.TotalBalance += convertedAmount;

        _db.Transactions.Add(new Transaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            Type = "credit",
            Amount = convertedAmount,
            BalanceAfter = wallet.TotalBalance,
            Description = taskCurrency != wallet.Currency
                ? $"Reward earned for completing task #{taskId} ({taskCurrency} {amount:F2} → {wallet.Currency} {convertedAmount:F2})"
                : $"Reward earned for completing task #{taskId}",
            ReferenceType = "TaskReward",
            ReferenceId = taskId,
            Status = StatusHelper.TransactionCompleted,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("Credited {Amount} {Currency} to wallet {WalletId} for task {TaskId} (proof {ProofId})",
            convertedAmount, wallet.Currency, wallet.Id, taskId, proofId);
    }

    public static decimal ConvertCurrencyStatic(decimal amount, string fromCurrency, string toCurrency) =>
        ConvertCurrency(amount, fromCurrency, toCurrency);

    private static decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        const decimal PkrPerUsd = 285m;

        return (fromCurrency.ToUpper(), toCurrency.ToUpper()) switch
        {
            ("USD", "PKR") => Math.Round(amount * PkrPerUsd, 2),
            ("PKR", "USD") => Math.Round(amount / PkrPerUsd, 2),
            _ => amount
        };
    }

    private async Task<Wallet> GetOrCreateWalletAsync(int userId)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                TotalBalance = 0,
                Currency = "USD",
                Withdrawn = 0,
                Status = StatusHelper.WalletActive,
                CreatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
        }
        return wallet;
    }

    public async Task WithdrawAsync(int userId, decimal amount)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return;

        wallet.TotalBalance -= amount;
        wallet.Withdrawn += amount;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateCurrencyAsync(int userId, string currency)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return false;

        wallet.Currency = currency;
        await _db.SaveChangesAsync();
        return true;
    }
}
