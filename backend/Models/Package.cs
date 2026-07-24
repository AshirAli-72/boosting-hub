using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("packages")]
public class Package
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("platform")] public string Platform { get; set; } = string.Empty;
    [Column("service")] public string Service { get; set; } = string.Empty;
    [Column("price")] public decimal Price { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}
