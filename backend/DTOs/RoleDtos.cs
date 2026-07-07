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
