using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("task_complete")]
public class TaskComplete
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("task_id")] public int TaskId { get; set; }

    // points to task_proofs.id
    [Column("proof_id")] public int? ProofId { get; set; }

    [Column("date")] public DateTime Date { get; set; } = DateTime.UtcNow;
    [Column("status", TypeName = "nvarchar(50)")] public string Status { get; set; } = "2";

    public User User { get; set; } = null!;
    public TaskGenerate Task { get; set; } = null!;
    public TaskProof? Proof { get; set; }
}

