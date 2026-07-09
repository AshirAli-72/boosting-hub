using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _db;

    public AccountService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<AccountDto>>> GetAccountsByUserIdAsync(int userId)
    {
        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                AccountTitle = a.AccountTitle,
                MobileNumber = a.MobileNumber,
                Cnic = a.Cnic,
                IsDefault = a.IsDefault,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Result.Success(accounts);
    }

    public async Task<Result<AccountDto>> CreateAccountAsync(int userId, CreateAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AccountTitle))
            return Result.Failure<AccountDto>("Account title is required");
        if (string.IsNullOrWhiteSpace(dto.MobileNumber))
            return Result.Failure<AccountDto>("Mobile number is required");
        if (string.IsNullOrWhiteSpace(dto.Cnic))
            return Result.Failure<AccountDto>("CNIC is required");

        if (dto.IsDefault)
        {
            var existingDefaults = await _db.Accounts
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();
            foreach (var d in existingDefaults)
                d.IsDefault = false;
        }

        var account = new Account
        {
            UserId = userId,
            AccountTitle = dto.AccountTitle,
            MobileNumber = dto.MobileNumber,
            Cnic = dto.Cnic,
            IsDefault = dto.IsDefault,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        return Result.Success(new AccountDto
        {
            Id = account.Id,
            AccountTitle = account.AccountTitle,
            MobileNumber = account.MobileNumber,
            Cnic = account.Cnic,
            IsDefault = account.IsDefault,
            Status = account.Status,
            CreatedAt = account.CreatedAt
        }, "Account created successfully");
    }

    public async Task<Result> DeleteAccountAsync(int userId, int accountId)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
        if (account == null)
            return Result.Failure("Account not found");

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();

        return Result.Success("Account deleted successfully");
    }
}
