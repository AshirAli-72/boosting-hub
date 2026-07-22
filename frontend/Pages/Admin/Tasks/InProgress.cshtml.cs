using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Tasks;

    public class InProgressModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public InProgressModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<TaskItem> Tasks { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    private const int PageSize = 10;

    public string GetCurrencySymbol(string? currency) => currency?.ToUpper() switch
    {
        "PKR" => "₨",
        "EUR" => "€",
        "GBP" => "£",
        "INR" => "₹",
        "BDT" => "৳",
        _ => "$"
    };

    public class TaskItem
    {
        public int Id { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int CompletedCount { get; set; }
        public decimal Reward { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime CreatedAt { get; set; }
    }

    public async Task OnGetAsync([FromQuery] int page = 1)
    {
        CurrentPage = page < 1 ? 1 : page;

        try
        {
            var completedCounts = await _db.TaskCompletes
                .Where(tc => tc.Status == StatusHelper.TaskCompleteCompleted)
                .GroupBy(tc => tc.TaskId)
                .Select(g => new { TaskId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TaskId, x => x.Count);

            var query = _db.TaskGenerates
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Platform,
                    t.Service,
                    t.Quantity,
                    t.Reward,
                    t.Currency,
                    t.CreatedAt,
                    CompletedCount = completedCounts.ContainsKey(t.Id) ? completedCounts[t.Id] : 0
                });

            var allItems = await query.ToListAsync();

            var filtered = allItems
                .Where(t => t.CompletedCount < t.Quantity)
                .ToList();

            TotalCount = filtered.Count;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0)
                CurrentPage = TotalPages;

            Tasks = filtered
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(t => new TaskItem
                {
                    Id = t.Id,
                    Platform = t.Platform,
                    Service = t.Service,
                    Quantity = t.Quantity,
                    CompletedCount = t.CompletedCount,
                    Reward = t.Reward,
                    Currency = t.Currency,
                    CreatedAt = t.CreatedAt
                })
                .ToList();
        }
        catch
        {
            Tasks = new List<TaskItem>();
        }
    }
}
