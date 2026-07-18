using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("task_proofs")]
public class TaskProof
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("task_id")] public int TaskId { get; set; }

    [Column("proof_url")] public string ProofUrl { get; set; } = string.Empty;
    [Column("proof_type")] public string ProofType { get; set; } = string.Empty;

    [Column("date")] public DateTime Date { get; set; } = DateTime.UtcNow;
    [Column("status")] public int Status { get; set; } = 2;
    [Column("verification_status")] public int VerificationStatus { get; set; } = 4;
    [Column("reject_reason")] public string? RejectReason { get; set; }

    public User User { get; set; } = null!;
    public TaskGenerate Task { get; set; } = null!;
}

