using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _db;

    public UserManagementService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<UserWithRolesDto>>> GetUsersAsync()
    {
        var users = await _db.Users
            .Include(u => u.UserHasRoles)
            .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var dtos = users.Select(u => new UserWithRolesDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Phone = u.Phone,
            Status = u.Status == 1 ? "Active" : "Inactive",
            EmailVerifiedAt = u.EmailVerifiedAt,
            Roles = u.UserHasRoles.Select(ur => new RoleBasicDto
            {
                Id = ur.Role!.Id,
                RoleTitle = ur.Role.RoleTitle
            }).ToList()
        }).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<UserWithRolesDto>> CreateUserAsync(CreateUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Result.Failure<UserWithRolesDto>("Email already registered", "DUPLICATE_EMAIL");

        if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone))
            return Result.Failure<UserWithRolesDto>("Phone already registered", "DUPLICATE_PHONE");

        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Password = hasher.HashPassword(null!, dto.Password),
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        foreach (var roleId in dto.RoleIds)
        {
            _db.UserHasRoles.Add(new UserHasRole { UserId = user.Id, RoleId = roleId });
        }

        await _db.SaveChangesAsync();

        var roleIdsSet = dto.RoleIds.ToHashSet();
        var allRoles = await _db.Roles.ToListAsync();
        var roles = allRoles.Where(r => roleIdsSet.Contains(r.Id)).ToList();

        return Result.Success(new UserWithRolesDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Status = "Active",
            Roles = roles.Select(r => new RoleBasicDto { Id = r.Id, RoleTitle = r.RoleTitle }).ToList()
        }, "User created successfully");
    }

    public async Task<Result> UpdateUserRolesAsync(int userId, List<int> roleIds)
    {
        var user = await _db.Users.Include(u => u.UserHasRoles).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Result.Failure("User not found", "NOT_FOUND");

        _db.UserHasRoles.RemoveRange(user.UserHasRoles);

        foreach (var roleId in roleIds)
        {
            _db.UserHasRoles.Add(new UserHasRole { UserId = user.Id, RoleId = roleId });
        }

        await _db.SaveChangesAsync();
        return Result.Success("User roles updated");
    }
}
