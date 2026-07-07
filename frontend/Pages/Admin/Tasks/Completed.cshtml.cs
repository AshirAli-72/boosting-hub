using BoostingHub.backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Tasks;

public class CompletedModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public CompletedModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<TaskItem> Tasks { get; set; } = new();

    public class TaskItem
    {
        public int Id { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int CompletedCount { get; set; }
        public decimal Reward { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public async Task OnGetAsync()
    {
        var completedCounts = await _db.TaskCompletes
            .Where(tc => tc.Status == "Completed")
            .GroupBy(tc => tc.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TaskId, x => x.Count);

        var tasks = await _db.TaskGenerates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        Tasks = tasks
            .Select(t =>
            {
                var done = completedCounts.GetValueOrDefault(t.Id, 0);
                return new TaskItem
                {
                    Id = t.Id,
                    Platform = t.Platform,
                    Service = t.Service,
                    Quantity = t.Quantity,
                    CompletedCount = done,
                    Reward = t.Reward,
                    CreatedAt = t.CreatedAt
                };
            })
            .Where(t => t.CompletedCount >= t.Quantity)
            .ToList();
    }
}
