using BoostingHub.backend.Models;

namespace BoostingHub.backend.Services.Interfaces;

public interface IWalletService
{
    Task<Wallet?> GetWalletByUserIdAsync(int userId);
    Task<Wallet> CreateOrUpdateWalletAsync(int userId, decimal totalBalance, string currency, decimal withdrawn, string status);
    Task AddRewardAsync(int userId, decimal amount);
    Task WithdrawAsync(int userId, decimal amount);
}
