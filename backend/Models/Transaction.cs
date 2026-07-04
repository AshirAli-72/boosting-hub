using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("transactions")]
public class Transaction
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("wallet_id")] public int WalletId { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("type")] public string Type { get; set; } = string.Empty;
    [Column("amount")] public decimal Amount { get; set; }
    [Column("balance_after")] public decimal BalanceAfter { get; set; }
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("reference_type")] public string? ReferenceType { get; set; }
    [Column("reference_id")] public int? ReferenceId { get; set; }
    [Column("status")] public string Status { get; set; } = "Completed";
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wallet Wallet { get; set; } = null!;
    public User User { get; set; } = null!;
}
