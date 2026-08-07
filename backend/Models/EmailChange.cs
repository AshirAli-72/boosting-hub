using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("email_changes")]
public class EmailChange
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("old_email", TypeName = "nvarchar(255)")] public string? OldEmail { get; set; }
    [Column("new_email", TypeName = "nvarchar(255)")] public string NewEmail { get; set; } = string.Empty;
    [Column("otp_code", TypeName = "nvarchar(500)")] public string OtpCode { get; set; } = string.Empty;
    [Column("is_used")] public bool IsUsed { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
