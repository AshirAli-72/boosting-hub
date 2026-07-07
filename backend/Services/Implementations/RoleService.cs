using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _db;

    public RoleService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<RoleWithPermissionsDto>>> GetRolesAsync()
    {
        var roles = await _db.Roles
            .Include(r => r.RoleHasPermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync();

        var dtos = roles.Select(r => new RoleWithPermissionsDto
        {
            Id = r.Id,
            RoleTitle = r.RoleTitle,
            Description = r.Description,
            Permissions = r.RoleHasPermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission!.Id,
                Names = rp.Permission.Names,
                Slugs = rp.Permission.Slugs,
                Assigned = true
            }).ToList()
        }).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync()
    {
        var permissions = await _db.Permissions.ToListAsync();
        var dtos = permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Names = p.Names,
            Slugs = p.Slugs,
            Assigned = false
        }).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<RoleWithPermissionsDto>> CreateRoleAsync(CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RoleTitle))
            return Result.Failure<RoleWithPermissionsDto>("Role title is required", "VALIDATION_ERROR");

        if (await _db.Roles.AnyAsync(r => r.RoleTitle == dto.RoleTitle))
            return Result.Failure<RoleWithPermissionsDto>("Role already exists", "DUPLICATE_ROLE");

        var role = new Role
        {
            RoleTitle = dto.RoleTitle,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        foreach (var permissionId in dto.PermissionIds)
        {
            _db.RolesHasPermissions.Add(new RoleHasPermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await _db.SaveChangesAsync();

        var permissions = await _db.Permissions
            .Where(p => dto.PermissionIds.Contains(p.Id))
            .ToListAsync();

        return Result.Success(new RoleWithPermissionsDto
        {
            Id = role.Id,
            RoleTitle = role.RoleTitle,
            Description = role.Description,
            Permissions = permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Names = p.Names,
                Slugs = p.Slugs,
                Assigned = true
            }).ToList()
        }, "Role created successfully");
    }

    public async Task<Result<RoleWithPermissionsDto>> UpdateRoleAsync(int roleId, UpdateRoleDto dto)
    {
        var role = await _db.Roles
            .Include(r => r.RoleHasPermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
            return Result.Failure<RoleWithPermissionsDto>("Role not found", "NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(dto.RoleTitle))
        {
            if (await _db.Roles.AnyAsync(r => r.RoleTitle == dto.RoleTitle && r.Id != roleId))
                return Result.Failure<RoleWithPermissionsDto>("Role title already taken", "DUPLICATE_ROLE");
            role.RoleTitle = dto.RoleTitle;
        }

        if (dto.Description != null)
            role.Description = dto.Description;

        _db.RolesHasPermissions.RemoveRange(role.RoleHasPermissions);

        foreach (var permissionId in dto.PermissionIds)
        {
            _db.RolesHasPermissions.Add(new RoleHasPermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await _db.SaveChangesAsync();

        var permissions = await _db.Permissions
            .Where(p => dto.PermissionIds.Contains(p.Id))
            .ToListAsync();

        return Result.Success(new RoleWithPermissionsDto
        {
            Id = role.Id,
            RoleTitle = role.RoleTitle,
            Description = role.Description,
            Permissions = permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Names = p.Names,
                Slugs = p.Slugs,
                Assigned = true
            }).ToList()
        }, "Role updated successfully");
    }

    public async Task<Result> DeleteRoleAsync(int roleId)
    {
        var role = await _db.Roles
            .Include(r => r.RoleHasPermissions)
            .Include(r => r.UserHasRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
            return Result.Failure("Role not found", "NOT_FOUND");

        if (role.UserHasRoles.Any())
            return Result.Failure("Cannot delete role assigned to users", "ROLE_IN_USE");

        _db.RolesHasPermissions.RemoveRange(role.RoleHasPermissions);
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();

        return Result.Success("Role deleted successfully");
    }
}
