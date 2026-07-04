using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("notifications")]
public class Notification
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("type")] public string Type { get; set; } = string.Empty;
    [Column("title")] public string Title { get; set; } = string.Empty;
    [Column("message")] public string Message { get; set; } = string.Empty;
    [Column("data")] public string? Data { get; set; }
    [Column("is_read")] public bool IsRead { get; set; }
    [Column("read_at")] public DateTime? ReadAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
