using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("users")]
public class User
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("name")] public string? Name { get; set; }
    [Column("email")] public string? Email { get; set; }
    [Column("phone")] public string? Phone { get; set; }
    [Column("password")] public string? Password { get; set; }
    [Column("status")] public int Status { get; set; } = 1;
    [Column("email_verified_at")] public DateTime? EmailVerifiedAt { get; set; }
    [Column("remember_token")] public string? RememberToken { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserHasRole> UserHasRoles { get; set; } = new List<UserHasRole>();
    public Wallet? Wallet { get; set; }
}
