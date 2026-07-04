using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("permissions")]
public class Permission
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("names")] public string? Names { get; set; }
    [Column("slugs")] public string? Slugs { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RoleHasPermission> RoleHasPermissions { get; set; } = new List<RoleHasPermission>();
}
