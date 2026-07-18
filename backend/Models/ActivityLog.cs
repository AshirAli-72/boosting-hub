using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("activity_logs")]
public class ActivityLog
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int? UserId { get; set; }
    [Column("user_name")] public string? UserName { get; set; }
    [Column("user_email")] public string? UserEmail { get; set; }
    [Column("user_role")] public string UserRole { get; set; } = string.Empty;
    [Column("event")] public string Event { get; set; } = string.Empty;
    [Column("description")] public string? Description { get; set; }
    [Column("subject_type")] public string SubjectType { get; set; } = string.Empty;
    [Column("subject_id")] public int? SubjectId { get; set; }
    [Column("subject_name")] public string? SubjectName { get; set; }
    [Column("old_values")] public string? OldValues { get; set; }
    [Column("new_values")] public string? NewValues { get; set; }
    [Column("ip_address")] public string? IpAddress { get; set; }
    [Column("user_agent")] public string? UserAgent { get; set; }
    [Column("batch_id")] public Guid? BatchId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
