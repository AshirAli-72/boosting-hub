using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin;

public class TasksModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public TasksModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<TaskItemDto> TaskList { get; set; } = new();
    public string CurrentStatus { get; set; } = "inprogress";

    public class TaskItemDto
    {
        public int Id { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int CompletedCount { get; set; }
        public decimal Reward { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public async Task OnGetAsync([FromQuery] string status = "")
    {
        CurrentStatus = status;

        var completedCounts = await _db.TaskCompletes
            .Where(tc => tc.Status == "Completed")
            .GroupBy(tc => tc.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TaskId, x => x.Count);

        var tasks = await _db.TaskGenerates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        TaskList = tasks.Select(t =>
        {
            var done = completedCounts.GetValueOrDefault(t.Id, 0);
            return new TaskItemDto
            {
                Id = t.Id,
                Platform = t.Platform,
                Service = t.Service,
                Quantity = t.Quantity,
                CompletedCount = done,
                Reward = t.Reward,
                CreatedAt = t.CreatedAt,
                Status = done >= t.Quantity ? "Completed" : "In Progress"
            };
        })
        .Where(t => string.IsNullOrEmpty(status) || (status == "completed" ? t.Status == "Completed" : t.Status == "In Progress"))
        .ToList();
    }
}
