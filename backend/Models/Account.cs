using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("accounts")]
public class Account
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("account_title")] public string AccountTitle { get; set; } = "";
    [Column("mobile_number")] public string MobileNumber { get; set; } = "";
    [Column("cnic")] public string Cnic { get; set; } = "";
    [Column("is_default")] public bool IsDefault { get; set; }
    [Column("status", TypeName = "nvarchar(20)")] public string Status { get; set; } = "1";
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
