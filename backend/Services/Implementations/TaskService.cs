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
    private readonly IWalletService _walletService;
    private const int MaxActiveTasks = 10;
    private const int DailyTaskLimit = 25;

    public TaskService(ApplicationDbContext db, ILogger<TaskService> logger, IWalletService walletService)
    {
        _db = db;
        _logger = logger;
        _walletService = walletService;
    }

    public async Task<PagedResult<AvailableTaskDto>> GetAvailableTasksAsync(TaskFilterDto filter, int? userId = null)
    {
        var completedCounts = await _db.TaskCompletes
            .Where(tc => tc.Status == "Completed")
            .GroupBy(tc => tc.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(tc => tc.TaskId, tc => tc.Count);

        var userStatusMap = await BuildUserStatusMapAsync(userId);

        var allTasks = await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == "Active")
            .ToListAsync();

        // Don't show "Completed" for a task until all slots are filled
        foreach (var kv in userStatusMap.Where(kv => kv.Value == "Completed"))
        {
            var completedCount = completedCounts.GetValueOrDefault(kv.Key, 0);
            var task = allTasks.FirstOrDefault(t => t.Id == kv.Key);
            if (task != null && completedCount < task.Quantity)
                userStatusMap[kv.Key] = "Accepted";
        }

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

    private async Task<Dictionary<int, string>> BuildUserStatusMapAsync(int? userId)
    {
        var map = new Dictionary<int, string>();
        if (!userId.HasValue) return map;

        var completions = await _db.TaskCompletes
            .Where(tc => tc.UserId == userId.Value)
            .Select(tc => new { tc.TaskId, tc.Status })
            .ToListAsync();
        foreach (var c in completions)
        {
            if (c.Status == "Completed")
                map[c.TaskId] = "Completed";
            else if (c.Status != "Cancelled" && !map.ContainsKey(c.TaskId))
                map[c.TaskId] = "Accepted";
        }

        var acceptedTaskIds = await _db.AcceptedTasks
            .Where(a => a.UserId == userId.Value)
            .Select(a => a.TaskId)
            .ToListAsync();
        foreach (var taskId in acceptedTaskIds)
        {
            if (!map.ContainsKey(taskId))
                map[taskId] = "Accepted";
        }

        return map;
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
            {
                var totalCompleted = await _db.TaskCompletes
                    .CountAsync(tc => tc.TaskId == taskId && tc.Status == "Completed");
                userStatus = totalCompleted >= task.Quantity ? "Completed" : "Accepted";
            }
            else if (completion != null && completion != "Cancelled")
                userStatus = "Accepted";
            else if (await _db.AcceptedTasks.AnyAsync(a => a.UserId == userId.Value && a.TaskId == taskId))
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
        var task = await _db.TaskGenerates.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            return Result.Failure<AcceptTaskResult>("Task not found", "NOT_FOUND");
        if (task.Status != "Active")
            return Result.Failure<AcceptTaskResult>("Task is no longer active", "INACTIVE");

        var worker = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (worker == null || worker.Status != 1)
            return Result.Failure<AcceptTaskResult>("Account is not active", "ACCOUNT_INACTIVE");

        var alreadyAccepted = await _db.AcceptedTasks.AnyAsync(a => a.UserId == userId && a.TaskId == taskId);
        if (alreadyAccepted)
            return Result.Failure<AcceptTaskResult>("You already accepted this task", "ALREADY_ACCEPTED");

        var alreadyCompleted = await _db.TaskCompletes.AnyAsync(tc => tc.TaskId == taskId && tc.UserId == userId);
        if (alreadyCompleted)
            return Result.Failure<AcceptTaskResult>("You already completed this task", "ALREADY_COMPLETED");

        _db.AcceptedTasks.Add(new AcceptedTask
        {
            UserId = userId,
            TaskId = taskId,
            AcceptedAt = DateTime.UtcNow,
            Status = "Accepted"
        });
        await _db.SaveChangesAsync();

        return Result.Success(new AcceptTaskResult
        {
            Success = true,
            Message = "Task accepted! Complete it and submit proof."
        });
    }

    public async Task<List<MyTaskDto>> GetMyTasksAsync(int userId)
    {
        try
        {
            var acceptedTaskIds = await _db.AcceptedTasks
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .ToListAsync();

            var proofs = await _db.TaskProofs
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.TaskId);

            var completions = await _db.TaskCompletes
                .Where(tc => tc.UserId == userId)
                .OrderByDescending(tc => tc.Date)
                .Select(tc => new { tc.TaskId, tc.Status, tc.Id })
                .ToListAsync();

            var completedTaskIds = completions
                .Where(c => c.Status == "Completed")
                .Select(c => c.TaskId)
                .ToHashSet();

            var allTaskIds = acceptedTaskIds
                .Union(completedTaskIds)
                .Union(completions.Select(c => c.TaskId))
                .ToList();

            if (allTaskIds.Count == 0)
                return new List<MyTaskDto>();

            var tasks = await _db.TaskGenerates
                .AsNoTracking()
                .Where(t => allTaskIds.Contains(t.Id))
                .ToListAsync();

            var result = new List<MyTaskDto>();

            foreach (var t in tasks)
            {
                var proof = proofs.GetValueOrDefault(t.Id);
                var comp = completions.FirstOrDefault(c => c.TaskId == t.Id);

                if (proof != null && comp == null)
                {
                    result.Add(new MyTaskDto
                    {
                        TaskId = t.Id,
                        Platform = t.Platform,
                        Service = t.Service,
                        Url = t.Url,
                        Reward = t.Reward,
                        Status = "Submitted",
                        ProofUrl = proof.ProofUrl,
                        ProofType = proof.ProofType,
                        ProofStatus = proof.Status
                    });
                }
                else if (comp != null)
                {
                    result.Add(new MyTaskDto
                    {
                        TaskCompleteId = comp.Id,
                        TaskId = t.Id,
                        Platform = t.Platform,
                        Service = t.Service,
                        Url = t.Url,
                        Reward = t.Reward,
                        Status = comp.Status,
                        ProofUrl = proof?.ProofUrl,
                        ProofType = proof?.ProofType,
                        ProofStatus = proof?.Status
                    });
                }
                else
                {
                    result.Add(new MyTaskDto
                    {
                        TaskId = t.Id,
                        Platform = t.Platform,
                        Service = t.Service,
                        Url = t.Url,
                        Reward = t.Reward,
                        Status = "Pending"
                    });
                }
            }

            _logger.LogInformation("GetMyTasksAsync returned {Count} results", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyTasksAsync failed for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Result> SubmitProofAsync(int taskId, string proofUrl, string proofType, int userId)
    {
        try
        {
            var task = await _db.TaskGenerates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return Result.Failure("Task not found", "NOT_FOUND");

            var accepted = await _db.AcceptedTasks
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TaskId == taskId);
            if (accepted == null)
                return Result.Failure("You have not accepted this task", "NOT_ACCEPTED");

            var existingProof = await _db.TaskProofs
                .AnyAsync(p => p.UserId == userId && p.TaskId == taskId);
            if (existingProof)
                return Result.Failure("Proof already submitted for this task", "ALREADY_SUBMITTED");

            var proof = new TaskProof
            {
                UserId = userId,
                TaskId = taskId,
                ProofUrl = proofUrl,
                ProofType = proofType,
                Date = DateTime.UtcNow,
                Status = "Completed"
            };

            _db.TaskProofs.Add(proof);
            await _db.SaveChangesAsync();

            _db.TaskCompletes.Add(new TaskComplete
            {
                UserId = userId,
                TaskId = taskId,
                ProofId = proof.Id,
                Date = DateTime.UtcNow,
                Status = "Completed"
            });

            await _db.SaveChangesAsync();

            await _walletService.AddRewardAsync(userId, task.Reward);

            _logger.LogInformation("Task {Id} auto-completed for user {UserId}, reward {Reward} added to wallet", taskId, userId, task.Reward);

            return Result.Success($"Task completed! ${task.Reward} added to your wallet.");
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
        var acceptedCount = await _db.AcceptedTasks
            .CountAsync(a => a.UserId == userId && a.Status == "Accepted");
        var pendingCount = await _db.TaskCompletes
            .CountAsync(tc => tc.UserId == userId && tc.Status == "Pending");
        return acceptedCount + pendingCount;
    }
}
