using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("roles_has_permissions")]
public class RoleHasPermission
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("role_id")] public int RoleId { get; set; }
    [Column("permission_id")] public int PermissionId { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
