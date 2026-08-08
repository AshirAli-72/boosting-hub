using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Admin.Tasks;

public class ExpiredModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogService _activityLog;

    public ExpiredModel(ApplicationDbContext db, IActivityLogService activityLog)
    {
        _db = db;
        _activityLog = activityLog;
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
        public string Currency { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = "Active";
    }

    public async Task<IActionResult> OnGetAsync([FromQuery] int page = 1)
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        CurrentPage = page < 1 ? 1 : page;

        try
        {
            var completedCounts = await _db.TaskCompletes
                .Where(tc => tc.Status == StatusHelper.TaskCompleteCompleted)
                .GroupBy(tc => tc.TaskId)
                .Select(g => new { TaskId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TaskId, x => x.Count);

            var now = DateTime.UtcNow;

            var query = _db.TaskGenerates
                .AsNoTracking()
                .Where(t => t.ExpiryDate.HasValue && t.ExpiryDate.Value <= now)
                .OrderBy(t => t.ExpiryDate)
                .Select(t => new
                {
                    t.Id,
                    t.Platform,
                    t.Service,
                    t.Quantity,
                    t.Reward,
                    t.CreatedAt,
                    t.ExpiryDate,
                    t.Status,
                    Currency = t.Order.Currency,
                    CompletedCount = completedCounts.ContainsKey(t.Id) ? completedCounts[t.Id] : 0
                });

            var allItems = await query.ToListAsync();

            TotalCount = allItems.Count;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0)
                CurrentPage = TotalPages;

            Tasks = allItems
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
                    CreatedAt = t.CreatedAt,
                    ExpiryDate = t.ExpiryDate,
                    Status = StatusHelper.TaskGenerateStatusToString(t.Status)
                })
                .ToList();
        }
        catch
        {
            Tasks = new List<TaskItem>();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostChangeExpiryAsync([FromForm] int id, [FromForm] int days)
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var task = await _db.TaskGenerates.FirstOrDefaultAsync(t => t.Id == id);
        if (task == null)
        {
            TempData["Error"] = "Task not found.";
            return RedirectToPage();
        }

        task.ExpiryDate = days == -1 ? (DateTime?)null : DateTime.UtcNow.AddDays(days);
        task.Status = StatusHelper.TaskGenerateActive;
        await _db.SaveChangesAsync();

        var userIdStr = HttpContext.Session.GetString("UserId");
        var adminId = int.TryParse(userIdStr, out var uid) ? uid : 0;
        var adminUser = adminId > 0
            ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == adminId)
            : null;

        await _activityLog.LogAsync(
            userId: adminId, userName: adminUser?.Name, userEmail: adminUser?.Email,
            userRole: "Admin", evt: "TaskExpiryExtended",
            description: $"Admin extended expiry date of task #{task.Id} ({task.Platform} - {task.Service}) to {(task.ExpiryDate?.ToString("yyyy-MM-dd") ?? "No Expiry")}",
            subjectType: "TaskGenerate", subjectId: task.Id, subjectName: $"{task.Platform} - {task.Service}",
            oldValues: null, newValues: System.Text.Json.JsonSerializer.Serialize(new { ExpiryDate = task.ExpiryDate, Status = task.Status }));

        TempData["Success"] = $"Expiry date of task #{task.Id} updated successfully.";
        return RedirectToPage();
    }
}
