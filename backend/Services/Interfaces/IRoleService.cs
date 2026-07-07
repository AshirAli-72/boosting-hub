using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IRoleService
{
    Task<Result<List<RoleWithPermissionsDto>>> GetRolesAsync();
    Task<Result<List<PermissionDto>>> GetPermissionsAsync();
    Task<Result<RoleWithPermissionsDto>> CreateRoleAsync(CreateRoleDto dto);
    Task<Result<RoleWithPermissionsDto>> UpdateRoleAsync(int roleId, UpdateRoleDto dto);
    Task<Result> DeleteRoleAsync(int roleId);
}
