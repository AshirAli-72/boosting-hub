using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("pending_registrations")]
public class PendingRegistration
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("name")] [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Column("email")] [MaxLength(255)] public string Email { get; set; } = string.Empty;
    [Column("phone")] [MaxLength(50)] public string Phone { get; set; } = string.Empty;
    [Column("password")] [MaxLength(500)] public string Password { get; set; } = string.Empty;
    [Column("token")] [MaxLength(500)] public string Token { get; set; } = string.Empty;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("expires_at")] public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
