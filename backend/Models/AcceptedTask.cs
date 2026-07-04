using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("accepted_tasks")]
public class AcceptedTask
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("task_id")] public int TaskId { get; set; }
    [Column("accepted_at")] public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    [Column("status")] public string Status { get; set; } = "Accepted";

    public User User { get; set; } = null!;
    public TaskGenerate Task { get; set; } = null!;
}
