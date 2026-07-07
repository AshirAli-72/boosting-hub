namespace BoostingHub.backend.DTOs;

public class PermissionDto
{
    public int Id { get; set; }
    public string? Names { get; set; }
    public string? Slugs { get; set; }
    public bool Assigned { get; set; }
}

public class RoleWithPermissionsDto
{
    public int Id { get; set; }
    public string RoleTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class CreateRoleDto
{
    public string RoleTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}

public class UpdateRoleDto
{
    public string? RoleTitle { get; set; }
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}

public class UserWithRolesDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Status { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public List<RoleBasicDto> Roles { get; set; } = new();
}

public class RoleBasicDto
{
    public int Id { get; set; }
    public string RoleTitle { get; set; } = string.Empty;
}

public class CreateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<int> RoleIds { get; set; } = new();
}
