using BoostingHub.backend.Models;

namespace BoostingHub.backend.Services.Interfaces;

public interface IWalletService
{
    Task<Wallet?> GetWalletByUserIdAsync(int userId);
    Task<Wallet> CreateOrUpdateWalletAsync(int userId, decimal totalBalance, string currency, decimal withdrawn, int status);
    Task AddRewardAsync(int userId, decimal amount);
    Task CreditRewardAsync(int userId, decimal amount, int taskId, int proofId, string taskCurrency = "USD");
    Task WithdrawAsync(int userId, decimal amount);
    Task<bool> UpdateCurrencyAsync(int userId, string currency);
}
