using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("wallets")]
public class Wallet
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("total_balance")] public decimal TotalBalance { get; set; }
    [Column("currency")] public string Currency { get; set; } = "USD";
    [Column("withdrawn")] public decimal Withdrawn { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("status")] public int Status { get; set; } = 1;

    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
