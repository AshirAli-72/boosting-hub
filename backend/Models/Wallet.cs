using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("wallets")]
public class Wallet
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("balance")] public decimal Balance { get; set; }
    [Column("pending_balance")] public decimal PendingBalance { get; set; }
    [Column("total_earned")] public decimal TotalEarned { get; set; }
    [Column("total_withdrawn")] public decimal TotalWithdrawn { get; set; }
    [Column("currency")] public string Currency { get; set; } = "USD";
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
