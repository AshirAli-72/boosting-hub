using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TaskService> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private const int MaxActiveTasks = 10;
    private const int DailyTaskLimit = 25;

    public TaskService(ApplicationDbContext db, ILogger<TaskService> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _db = db;
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task<PagedResult<AvailableTaskDto>> GetAvailableTasksAsync(TaskFilterDto filter, int? userId = null)
    {
        var completedCounts = await _db.TaskCompletes
            .Where(tc => tc.Status == "Completed")
            .GroupBy(tc => tc.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(tc => tc.TaskId, tc => tc.Count);

        var userStatusMap = new Dictionary<int, string>();
        if (userId.HasValue)
        {
            var completions = await _db.TaskCompletes
                .Where(tc => tc.UserId == userId.Value)
                .Select(tc => new { tc.TaskId, tc.Status })
                .ToListAsync();
            foreach (var c in completions)
            {
                if (c.Status == "Completed")
                    userStatusMap[c.TaskId] = "Completed";
                else if (c.Status != "Cancelled" && !userStatusMap.ContainsKey(c.TaskId))
                    userStatusMap[c.TaskId] = "Accepted";
            }
        }

        var allTasks = await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == "Active")
            .ToListAsync();

        var filtered = allTasks
            .Where(t => t.Quantity > (completedCounts.GetValueOrDefault(t.Id, 0)));

        if (!string.IsNullOrEmpty(filter.Platform))
            filtered = filtered.Where(t => t.Platform == filter.Platform);

        if (!string.IsNullOrEmpty(filter.Service))
            filtered = filtered.Where(t => t.Service == filter.Service);

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.ToLower();
            filtered = filtered.Where(t => t.Platform.ToLower().Contains(search)
                                        || t.Service.ToLower().Contains(search));
        }

        if (filter.MinReward.HasValue)
            filtered = filtered.Where(t => t.Reward >= filter.MinReward.Value);

        if (filter.MaxReward.HasValue)
            filtered = filtered.Where(t => t.Reward <= filter.MaxReward.Value);

        var sorted = filter.SortBy.ToLower() switch
        {
            "reward" => filtered.OrderByDescending(t => t.Reward).ToList(),
            _ => filtered.OrderByDescending(t => t.CreatedAt).ToList()
        };

        var totalCount = sorted.Count;

        var paged = sorted
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var tasks = paged.Select(t => new AvailableTaskDto
        {
            Id = t.Id,
            OrderId = t.OrderId,
            Platform = t.Platform,
            Service = t.Service,
            Url = t.Url,
            Title = t.Service,
            TargetQuantity = t.Quantity,
            CompletedQuantity = completedCounts.GetValueOrDefault(t.Id, 0),
            RewardAmount = t.Reward,
            UserStatus = userId.HasValue ? userStatusMap.GetValueOrDefault(t.Id, "Not Accepted") : "Not Accepted",
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            ExpiresAt = t.CreatedAt.AddDays(3)
        }).ToList();

        return new PagedResult<AvailableTaskDto>
        {
            Items = tasks,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Result<TaskDetailDto>> GetTaskDetailAsync(int taskId, int? userId = null)
    {
        var task = await _db.TaskGenerates
            .AsNoTracking()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return Result.Failure<TaskDetailDto>("Task not found", "NOT_FOUND");

        if (task.Status != "Active")
            return Result.Failure<TaskDetailDto>("Task is no longer available", "INACTIVE");

        var completedCount = await _db.TaskCompletes
            .CountAsync(tc => tc.TaskId == taskId && tc.Status == "Completed");

        string userStatus = "Not Accepted";
        if (userId.HasValue)
        {
            var completion = await _db.TaskCompletes
                .Where(tc => tc.TaskId == taskId && tc.UserId == userId.Value)
                .Select(tc => tc.Status)
                .FirstOrDefaultAsync();
            if (completion == "Completed")
                userStatus = "Completed";
            else if (completion != null && completion != "Cancelled")
                userStatus = "Accepted";
        }

        return Result.Success(new TaskDetailDto
        {
            Id = task.Id,
            OrderId = task.OrderId,
            Platform = task.Platform,
            Service = task.Service,
            Url = task.Url,
            Title = task.Service,
            TargetQuantity = task.Quantity,
            CompletedQuantity = completedCount,
            RewardAmount = task.Reward,
            Description = task.Order?.Description ?? string.Empty,
            UserStatus = userStatus,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            ExpiresAt = task.CreatedAt.AddDays(3)
        });
    }

    public async Task<Result<AcceptTaskResult>> AcceptTaskAsync(int taskId, int userId)
    {
        var task = await _db.TaskGenerates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            return Result.Failure<AcceptTaskResult>("Task not found", "NOT_FOUND");
        if (task.Status != "Active")
            return Result.Failure<AcceptTaskResult>("Task is no longer active", "INACTIVE");

        var worker = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (worker == null || worker.Status != 1)
            return Result.Failure<AcceptTaskResult>("Account is not active", "ACCOUNT_INACTIVE");

        return Result.Success(new AcceptTaskResult
        {
            Success = true,
            Message = "Task accepted! Complete it and submit proof.",
            TaskCompleteId = null
        });
    }

    public async Task<List<MyTaskDto>> GetMyTasksAsync(int userId)
    {
        var completedTaskIds = await _db.TaskCompletes
            .Where(tc => tc.UserId == userId)
            .Select(tc => tc.TaskId)
            .ToListAsync();

        return await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == "Active" && !completedTaskIds.Contains(t.Id))
            .Include(t => t.Order)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new MyTaskDto

            {
                TaskCompleteId = 0,
                TaskId = t.Id,
                Platform = t.Platform,
                Service = t.Service,
                Url = t.Url,
                Reward = t.Reward,
                Status = "Pending",
                AcceptedAt = DateTime.MinValue,
                ProofUrl = null,
                ProofType = null,
                ProofStatus = null

            })
            .ToListAsync();
    }

    public async Task<Result> SubmitProofAsync(int taskId, string proofUrl, string proofType, int userId)
    {
        try
        {
            var task = await _db.TaskGenerates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return Result.Failure("Task not found", "NOT_FOUND");

            var proof = new TaskProof
            {
                UserId = userId,
                TaskId = taskId,
                ProofUrl = proofUrl,
                ProofType = proofType,
                Date = DateTime.UtcNow,
                Status = "Submitted"
            };

            _db.TaskProofs.Add(proof);
            await _db.SaveChangesAsync();

            var tc = new TaskComplete
            {
                TaskId = taskId,
                UserId = userId,
                ProofId = proof.Id,
                Date = DateTime.UtcNow,
                Status = "Completed"
            };

            _db.TaskCompletes.Add(tc);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Proof submitted for task {Id} by user {UserId}", taskId, userId);

            return Result.Success("Proof submitted! Task marked as completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit proof for task {Id} by user {UserId}", taskId, userId);
            return Result.Failure($"Error: {ex.Message}", "ERROR");
        }
    }

    public async Task<List<string>> GetPlatformsAsync()
    {
        return await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == "Active")
            .Select(t => t.Platform)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetServicesAsync()
    {
        return await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == "Active")
            .Select(t => t.Service)
            .Distinct()
            .ToListAsync();
    }

    public async Task<int> GetWorkerActiveTaskCountAsync(int userId)
    {
        return await _db.TaskCompletes
            .CountAsync(tc => tc.UserId == userId && tc.Status == "Pending");
    }
}
