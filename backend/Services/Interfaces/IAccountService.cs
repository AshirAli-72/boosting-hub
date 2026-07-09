using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IAccountService
{
    Task<Result<List<AccountDto>>> GetAccountsByUserIdAsync(int userId);
    Task<Result<AccountDto>> CreateAccountAsync(int userId, CreateAccountDto dto);
    Task<Result> DeleteAccountAsync(int userId, int accountId);
}
