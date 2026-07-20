using System.Text.Json;
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
    private readonly IActivityLogService _activityLog;

    public WalletService(ApplicationDbContext db, ILogger<WalletService> logger, IActivityLogService activityLog)
    {
        _db = db;
        _logger = logger;
        _activityLog = activityLog;
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

        var creditUser = await _db.Users.FindAsync(userId);
        await _activityLog.LogAsync(
            userId: userId, userName: creditUser?.Name, userEmail: creditUser?.Email,
            userRole: "System", evt: "WalletCredited",
            description: $"{wallet.Currency} {convertedAmount:F2} credited for task #{taskId}",
            subjectType: "Wallet", subjectId: wallet.Id, subjectName: creditUser?.Email,
            newValues: JsonSerializer.Serialize(new { Amount = convertedAmount, Currency = wallet.Currency, BalanceAfter = wallet.TotalBalance, TaskId = taskId }),
            ct: CancellationToken.None);
    }

    public static decimal ConvertCurrencyStatic(decimal amount, string fromCurrency, string toCurrency) =>
        ConvertCurrency(amount, fromCurrency, toCurrency);

    private static decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 1m,
            ["EUR"] = 0.92m,
            ["GBP"] = 0.79m,
            ["PKR"] = 285m,
            ["INR"] = 83.12m,
            ["BDT"] = 109.85m,
        };

        if (!rates.TryGetValue(fromCurrency, out var fromRate) || !rates.TryGetValue(toCurrency, out var toRate))
            return amount;

        var inUsd = amount / fromRate;
        return Math.Round(inUsd * toRate, 2);
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

        var withdrawUser = await _db.Users.FindAsync(userId);
        await _activityLog.LogAsync(
            userId: userId, userName: withdrawUser?.Name, userEmail: withdrawUser?.Email,
            userRole: "User", evt: "WalletWithdrawn",
            description: $"{wallet.Currency} {amount:F2} withdrawn from wallet",
            subjectType: "Wallet", subjectId: wallet.Id, subjectName: withdrawUser?.Email,
            newValues: JsonSerializer.Serialize(new { Amount = amount, Currency = wallet.Currency, BalanceAfter = wallet.TotalBalance }),
            ct: CancellationToken.None);
    }

    public async Task<bool> UpdateCurrencyAsync(int userId, string currency)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return false;

        var oldCurrency = wallet.Currency;
        if (!string.Equals(oldCurrency, currency, StringComparison.OrdinalIgnoreCase))
        {
            wallet.TotalBalance = ConvertCurrency(wallet.TotalBalance, oldCurrency, currency);
            wallet.Withdrawn = ConvertCurrency(wallet.Withdrawn, oldCurrency, currency);
        }
        wallet.Currency = currency;
        await _db.SaveChangesAsync();
        return true;
    }
}
