using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("user_has_roles")]
public class UserHasRole
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("role_id")] public int RoleId { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}
