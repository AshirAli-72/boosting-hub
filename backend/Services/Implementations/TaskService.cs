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
    private readonly IProofVerificationService _proofVerification;
    private const int MaxActiveTasks = 10;
    private const int DailyTaskLimit = 25;

    public TaskService(
        ApplicationDbContext db,
        ILogger<TaskService> logger,
        IWalletService walletService,
        IProofVerificationService proofVerification)
    {
        _db = db;
        _logger = logger;
        _walletService = walletService;
        _proofVerification = proofVerification;
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
            Currency = t.Currency,
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
            Currency = task.Currency,
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

            var proofsList = await _db.TaskProofs
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            var proofs = proofsList
                .GroupBy(p => p.TaskId)
                .ToDictionary(g => g.Key, g => g.First());

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

            var allTasks = await _db.TaskGenerates.AsNoTracking().ToListAsync();
            var tasks = allTasks.Where(t => allTaskIds.Contains(t.Id)).ToList();

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
                        Currency = t.Currency,
                        Status = proof.VerificationStatus == "Rejected" ? "Rejected" : "Submitted",
                        ProofUrl = proof.ProofUrl,
                        ProofType = proof.ProofType,
                        ProofStatus = proof.Status,
                        VerificationStatus = proof.VerificationStatus,
                        RejectReason = proof.RejectReason
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
                        Currency = t.Currency,
                        Status = comp.Status,
                        ProofUrl = proof?.ProofUrl,
                        ProofType = proof?.ProofType,
                        ProofStatus = proof?.Status,
                        VerificationStatus = proof?.VerificationStatus,
                        RejectReason = proof?.RejectReason
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
                        Currency = t.Currency,
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

            if (task.Status != "Active")
                return Result.Failure("Task has expired or is no longer available", "TASK_EXPIRED");

            var existingProof = await _db.TaskProofs
                .FirstOrDefaultAsync(p => p.UserId == userId && p.TaskId == taskId && p.VerificationStatus != "Rejected");
            if (existingProof != null)
                return Result.Failure("You have already submitted a proof for this task", "ALREADY_SUBMITTED");

            var verification = await _proofVerification.ValidateProofAsync(taskId, proofUrl, userId);

            var proof = new TaskProof
            {
                UserId = userId,
                TaskId = taskId,
                ProofUrl = proofUrl,
                ProofType = proofType,
                Date = DateTime.UtcNow,
                Status = "Submitted",
                VerificationStatus = verification.Success ? "PendingReview" : "Rejected",
                RejectReason = verification.Success ? null : verification.ErrorMessage
            };

            _db.TaskProofs.Add(proof);
            await _db.SaveChangesAsync();

            if (!verification.Success)
            {
                _logger.LogInformation("Proof for task {TaskId} by user {UserId} rejected by auto-validation: {Reason}",
                    taskId, userId, verification.ErrorMessage);
                return Result.Failure($"Proof rejected: {verification.ErrorMessage}", "VERIFICATION_FAILED");
            }

            await NotifyAdminsAsync(proof.Id, userId, taskId);

            _logger.LogInformation("Proof for task {TaskId} by user {UserId} passed auto-validation, pending admin review", taskId, userId);

            return Result.Success("Proof submitted successfully and is pending admin review.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit proof for task {Id} by user {UserId}", taskId, userId);
            return Result.Failure($"Error: {ex.Message}", "ERROR");
        }
    }

    public async Task<List<ProofReviewDto>> GetProofsPendingReviewAsync()
    {
        return await _db.TaskProofs
            .AsNoTracking()
            .Where(p => p.VerificationStatus == "PendingReview")
            .Include(p => p.User)
            .Include(p => p.Task)
            .OrderByDescending(p => p.Date)
            .Select(p => new ProofReviewDto
            {
                ProofId = p.Id,
                TaskId = p.TaskId,
                UserId = p.UserId,
                UserName = p.User!.Name ?? string.Empty,
                ProofUrl = p.ProofUrl,
                Platform = p.Task!.Platform,
                Service = p.Task.Service,
                TaskUrl = p.Task.Url,
                Reward = p.Task.Reward,
                Currency = p.Task.Currency,
                SubmittedAt = p.Date,
                VerificationStatus = p.VerificationStatus,
                RejectReason = p.RejectReason
            })
            .ToListAsync();
    }

    public async Task<Result> ApproveProofAsync(int proofId)
    {
        try
        {
            var proof = await _db.TaskProofs
                .Include(p => p.Task)
                .FirstOrDefaultAsync(p => p.Id == proofId);

            if (proof == null)
                return Result.Failure("Proof not found", "NOT_FOUND");

            if (proof.VerificationStatus != "PendingReview")
                return Result.Failure("Proof is not pending review", "INVALID_STATUS");

            var alreadyCompleted = await _db.TaskCompletes
                .AnyAsync(tc => tc.TaskId == proof.TaskId && tc.UserId == proof.UserId && tc.Status == "Completed");

            if (!alreadyCompleted)
            {
                _db.TaskCompletes.Add(new TaskComplete
                {
                    UserId = proof.UserId,
                    TaskId = proof.TaskId,
                    ProofId = proof.Id,
                    Date = DateTime.UtcNow,
                    Status = "Completed"
                });
            }

            proof.VerificationStatus = "Approved";
            proof.Status = "Completed";

            await _db.SaveChangesAsync();

            await _walletService.CreditRewardAsync(proof.UserId, proof.Task.Reward, proof.TaskId, proof.Id, proof.Task.Currency);

            var wallet = await _walletService.GetWalletByUserIdAsync(proof.UserId);
            var displayCurrency = wallet?.Currency ?? "USD";
            var displayAmount = wallet != null
                ? WalletService.ConvertCurrencyStatic(proof.Task.Reward, proof.Task.Currency, displayCurrency)
                : proof.Task.Reward;

            _db.Notifications.Add(new Notification
            {
                UserId = proof.UserId,
                Type = "ProofApproved",
                Title = "Proof Approved",
                Message = $"Your proof for task #{proof.TaskId} has been approved. {displayCurrency} {displayAmount:F2} credited to your wallet.",
                Data = $"{{\"proofId\":{proofId},\"taskId\":{proof.TaskId}}}",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            _logger.LogInformation("Proof {ProofId} approved; task {TaskId} completed for user {UserId}, reward {Reward} credited",
                proofId, proof.TaskId, proof.UserId, proof.Task.Reward);

            return Result.Success($"Proof approved! {displayCurrency} {displayAmount:F2} credited to worker's wallet.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve proof {ProofId}", proofId);
            return Result.Failure($"Error: {ex.Message}", "ERROR");
        }
    }

    public async Task<Result> RejectProofAsync(int proofId, string reason)
    {
        try
        {
            var proof = await _db.TaskProofs.FindAsync(proofId);
            if (proof == null)
                return Result.Failure("Proof not found", "NOT_FOUND");

            if (proof.VerificationStatus != "PendingReview")
                return Result.Failure("Proof is not pending review", "INVALID_STATUS");

            proof.VerificationStatus = "Rejected";
            proof.RejectReason = reason ?? "Rejected by admin";
            proof.Status = "Rejected";

            await _db.SaveChangesAsync();

            _db.Notifications.Add(new Notification
            {
                UserId = proof.UserId,
                Type = "ProofRejected",
                Title = "Proof Rejected",
                Message = $"Your proof for task #{proof.TaskId} has been rejected. Reason: {proof.RejectReason}",
                Data = $"{{\"proofId\":{proofId},\"taskId\":{proof.TaskId}}}",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            _logger.LogInformation("Proof {ProofId} rejected by admin: {Reason}", proofId, reason);

            return Result.Success("Proof rejected.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject proof {ProofId}", proofId);
            return Result.Failure($"Error: {ex.Message}", "ERROR");
        }
    }

    private async Task NotifyAdminsAsync(int proofId, int userId, int taskId)
    {
        try
        {
            var adminUserIds = await _db.UserHasRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.Role!.RoleTitle == "Admin" || ur.Role.RoleTitle == "Administrator")
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            if (adminUserIds.Count == 0)
            {
                var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
                if (adminUser != null)
                    adminUserIds.Add(adminUser.Id);
            }

            foreach (var adminId in adminUserIds)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    Type = "ProofReview",
                    Title = "Proof Pending Review",
                    Message = $"Proof #{proofId} submitted by user #{userId} for task #{taskId} is pending your review.",
                    Data = $"{{\"proofId\":{proofId},\"taskId\":{taskId},\"userId\":{userId}}}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (adminUserIds.Count > 0)
                await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admins about proof {ProofId}", proofId);
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
