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
        var query = _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == "Active");

        if (!string.IsNullOrEmpty(filter.Platform))
            query = query.Where(t => t.Platform == filter.Platform);

        if (!string.IsNullOrEmpty(filter.Service))
            query = query.Where(t => t.Service == filter.Service);

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(t => t.Platform.ToLower().Contains(search)
                                  || t.Service.ToLower().Contains(search));
        }

        if (filter.MinReward.HasValue)
            query = query.Where(t => t.Reward >= filter.MinReward.Value);

        if (filter.MaxReward.HasValue)
            query = query.Where(t => t.Reward <= filter.MaxReward.Value);

        // Filter out tasks that have reached their target quantity
        query = query.Where(t => t.Quantity > _db.TaskCompletes.Count(tc => tc.TaskId == t.Id && tc.Status == "Completed"));

        // Sort
        query = filter.SortBy.ToLower() switch
        {
            "reward" => query.OrderByDescending(t => t.Reward),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var pagedTasks = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new 
            {
                Task = t,
                CompletedQuantity = _db.TaskCompletes.Count(tc => tc.TaskId == t.Id && tc.Status == "Completed")
            })
            .ToListAsync();

        var pagedTaskIds = pagedTasks.Select(x => x.Task.Id).ToList();
        var userStatusMap = await BuildUserStatusMapAsync(userId, pagedTaskIds);

        var tasks = pagedTasks.Select(x => 
        {
            var t = x.Task;
            var completedCount = x.CompletedQuantity;
            string userStatus = userId.HasValue ? userStatusMap.GetValueOrDefault(t.Id, "Not Accepted") : "Not Accepted";
            
            // Replicating original logic: if user is completed but task has capacity, revert to accepted
            if (userStatus == "Completed" && completedCount < t.Quantity)
                userStatus = "Accepted";

            return new AvailableTaskDto
            {
                Id = t.Id,
                OrderId = t.OrderId,
                Platform = t.Platform,
                Service = t.Service,
                Url = t.Url,
                Title = t.Service,
                TargetQuantity = t.Quantity,
                CompletedQuantity = completedCount,
                RewardAmount = t.Reward,
                Currency = t.Currency,
                UserStatus = userStatus,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                ExpiresAt = t.CreatedAt.AddDays(3)
            };
        }).ToList();

        return new PagedResult<AvailableTaskDto>
        {
            Items = tasks,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    private async Task<Dictionary<int, string>> BuildUserStatusMapAsync(int? userId, IEnumerable<int> taskIds)
    {
        var map = new Dictionary<int, string>();
        if (!userId.HasValue || !taskIds.Any()) return map;

        var completions = await _db.TaskCompletes
            .Where(tc => tc.UserId == userId.Value && taskIds.Contains(tc.TaskId))
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
            .Where(a => a.UserId == userId.Value && taskIds.Contains(a.TaskId))
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
            var myTasksQuery = _db.TaskGenerates
                .AsNoTracking()
                .Where(t => _db.AcceptedTasks.Any(a => a.UserId == userId && a.TaskId == t.Id) ||
                            _db.TaskCompletes.Any(c => c.UserId == userId && c.TaskId == t.Id) ||
                            _db.TaskProofs.Any(p => p.UserId == userId && p.TaskId == t.Id))
                .OrderByDescending(t => t.Id)
                .Take(100);

            var tasks = await myTasksQuery.ToListAsync();

            if (tasks.Count == 0)
                return new List<MyTaskDto>();

            var taskIds = tasks.Select(t => t.Id).ToList();

            var proofsList = await _db.TaskProofs
                .Where(p => p.UserId == userId && taskIds.Contains(p.TaskId))
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            var proofs = proofsList
                .GroupBy(p => p.TaskId)
                .ToDictionary(g => g.Key, g => g.First());

            var completions = await _db.TaskCompletes
                .Where(tc => tc.UserId == userId && taskIds.Contains(tc.TaskId))
                .OrderByDescending(tc => tc.Date)
                .Select(tc => new { tc.TaskId, tc.Status, tc.Id })
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
            .Take(100)
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
