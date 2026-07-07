using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IUserManagementService
{
    Task<Result<List<UserWithRolesDto>>> GetUsersAsync();
    Task<Result<UserWithRolesDto>> CreateUserAsync(CreateUserDto dto);
    Task<Result> UpdateUserRolesAsync(int userId, List<int> roleIds);
}
