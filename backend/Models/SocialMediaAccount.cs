using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("social_media_accounts")]
public class SocialMediaAccount
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("platform")] public string Platform { get; set; } = string.Empty;
    [Column("username")] public string Username { get; set; } = string.Empty;
    [Column("profile_url")] public string? ProfileUrl { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
