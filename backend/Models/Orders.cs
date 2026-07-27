using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("orders")]
public class Orders
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("full_name")] public string? FullName { get; set; }
    [Column("email")] public string? Email { get; set; }
    [Column("platform")] public string Platform { get; set; } = string.Empty;
    [Column("service")] public string Service { get; set; } = string.Empty;
    
    [Column("social_media_url")] public string? SocialMediaUrl { get; set; }
    [Column("description")] public string? Description { get; set; }

    // migration: orders.quantity is nvarchar(1000) (nullable)
    [Column("quantity")] public string? Quantity { get; set; }
    [Column("total_amount", TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Column("currency", TypeName = "nvarchar(10)")] public string Currency { get; set; } = string.Empty;
    [Column("status")] public int Status { get; set; } = 2;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("attachment")] public string? Attachment { get; set; }
    [Column("reject_reason")] public string? RejectReason { get; set; }

    [NotMapped] public decimal Budget { get; set; }

    public ICollection<TaskGenerate> TaskGenerates { get; set; } = new List<TaskGenerate>();
}
