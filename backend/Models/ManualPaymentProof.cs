using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("manual_payment_proofs")]
public class ManualPaymentProof
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("order_id")] public int OrderId { get; set; }
    [Column("paid_amount", TypeName = "decimal(18,2)")] public decimal PaidAmount { get; set; }
    [Column("paid_voucher")] public string? PaidVoucher { get; set; }
    [Column("submit_date")] public DateTime SubmitDate { get; set; } = DateTime.UtcNow;
    [Column("payment_method", TypeName = "nvarchar(50)")] public string PaymentMethod { get; set; } = string.Empty;
    [Column("status")] public int Status { get; set; } = 1;

    public Orders Order { get; set; } = null!;
}
