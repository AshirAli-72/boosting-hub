using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("task_generate")]
public class TaskGenerate
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("order_id")] public int OrderId { get; set; }
    [Column("platform")] public string Platform { get; set; } = string.Empty;
    [Column("service")] public string Service { get; set; } = string.Empty;
    [Column("quantity")] public int Quantity { get; set; }
    [Column("url")] public string Url { get; set; } = string.Empty;
    [Column("reward")] public decimal Reward { get; set; }
    [Column("currency", TypeName = "nvarchar(10)")] public string Currency { get; set; } = "USD";
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("status")] public string Status { get; set; } = "Active";

    public Orders Order { get; set; } = null!;
    public ICollection<TaskComplete> TaskCompletes { get; set; } = new List<TaskComplete>();
}
