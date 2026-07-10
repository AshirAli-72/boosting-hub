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
    [Column("budget")] public decimal Budget { get; set; }
    [Column("currency")] public string Currency { get; set; } = "USD";
    [Column("status")] public string Status { get; set; } = "Pending";
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaskGenerate> TaskGenerates { get; set; } = new List<TaskGenerate>();
}
