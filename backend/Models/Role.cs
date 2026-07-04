using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("roles")]
public class Role
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("role_title")] public string RoleTitle { get; set; } = string.Empty;
    [Column("description")] public string? Description { get; set; }
    [Column("created_at")] public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public ICollection<RoleHasPermission> RoleHasPermissions { get; set; } = new List<RoleHasPermission>();
    public ICollection<UserHasRole> UserHasRoles { get; set; } = new List<UserHasRole>();
}
