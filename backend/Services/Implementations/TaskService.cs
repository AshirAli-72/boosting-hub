using System.Text.Json;
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
    private readonly IActivityLogService _activityLog;
    private const int MaxActiveTasks = 10;
    private const int DailyTaskLimit = 25;

    public TaskService(
        ApplicationDbContext db,
        ILogger<TaskService> logger,
        IWalletService walletService,
        IProofVerificationService proofVerification,
        IActivityLogService activityLog)
    {
        _db = db;
        _logger = logger;
        _walletService = walletService;
        _proofVerification = proofVerification;
        _activityLog = activityLog;
    }

    public async Task<PagedResult<AvailableTaskDto>> GetAvailableTasksAsync(TaskFilterDto filter, int? userId = null)
    {
        var allActiveTasks = await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == StatusHelper.TaskGenerateActive)
            .ToListAsync();

        if (!string.IsNullOrEmpty(filter.Platform))
            allActiveTasks = allActiveTasks.Where(t => t.Platform == filter.Platform).ToList();
        if (!string.IsNullOrEmpty(filter.Service))
            allActiveTasks = allActiveTasks.Where(t => t.Service == filter.Service).ToList();
        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.ToLower();
            allActiveTasks = allActiveTasks.Where(t =>
                (t.Platform != null && t.Platform.ToLower().Contains(search)) ||
                (t.Service != null && t.Service.ToLower().Contains(search))).ToList();
        }
        if (filter.MinReward.HasValue)
            allActiveTasks = allActiveTasks.Where(t => t.Reward >= filter.MinReward.Value).ToList();
        if (filter.MaxReward.HasValue)
            allActiveTasks = allActiveTasks.Where(t => t.Reward <= filter.MaxReward.Value).ToList();

        var activeTaskIds = allActiveTasks.Select(t => t.Id).ToList();
        var allCompletedTaskIds = await _db.TaskCompletes
            .Where(tc => tc.Status == StatusHelper.TaskCompleteCompleted)
            .Select(tc => tc.TaskId)
            .ToListAsync();
        var completedCounts = allCompletedTaskIds
            .GroupBy(id => id)
            .Where(g => activeTaskIds.Contains(g.Key))
            .ToDictionary(g => g.Key, g => g.Count());

        allActiveTasks = allActiveTasks
            .Where(t => t.Quantity > completedCounts.GetValueOrDefault(t.Id, 0))
            .ToList();

        allActiveTasks = filter.SortBy.ToLower() switch
        {
            "reward" => allActiveTasks.OrderByDescending(t => t.Reward).ToList(),
            _ => allActiveTasks.OrderByDescending(t => t.CreatedAt).ToList()
        };

        var totalCount = allActiveTasks.Count;
        var pagedTasks = allActiveTasks
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var pagedTaskIds = pagedTasks.Select(t => t.Id).ToList();
        var userStatusMap = await BuildUserStatusMapAsync(userId, pagedTaskIds);

        var tasks = pagedTasks.Select(t =>
        {
            var completedCount = completedCounts.GetValueOrDefault(t.Id, 0);
            string userStatus = userId.HasValue ? userStatusMap.GetValueOrDefault(t.Id, "Not Accepted") : "Not Accepted";

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
                Status = StatusHelper.TaskGenerateStatusToString(t.Status),
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
        var taskIdSet = taskIds.ToHashSet();
        if (!userId.HasValue || taskIdSet.Count == 0) return map;

        var completions = await _db.TaskCompletes
            .Where(tc => tc.UserId == userId.Value)
            .Select(tc => new { tc.TaskId, tc.Status })
            .ToListAsync();
        foreach (var c in completions.Where(c => taskIdSet.Contains(c.TaskId)))
        {
            if (c.Status == StatusHelper.TaskCompleteCompleted)
                map[c.TaskId] = "Completed";
            else if (c.Status != StatusHelper.TaskCompleteCancelled && !map.ContainsKey(c.TaskId))
                map[c.TaskId] = "Accepted";
        }

        var acceptedTaskIds = await _db.AcceptedTasks
            .Where(a => a.UserId == userId.Value)
            .Select(a => a.TaskId)
            .ToListAsync();
        foreach (var taskId in acceptedTaskIds.Where(id => taskIdSet.Contains(id)))
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

        if (task.Status != StatusHelper.TaskGenerateActive)
            return Result.Failure<TaskDetailDto>("Task is no longer available", "INACTIVE");

        var completedCount = await _db.TaskCompletes
            .CountAsync(tc => tc.TaskId == taskId && tc.Status == StatusHelper.TaskCompleteCompleted);

        string userStatus = "Not Accepted";
        if (userId.HasValue)
        {
            var completion = await _db.TaskCompletes
                .Where(tc => tc.TaskId == taskId && tc.UserId == userId.Value)
                .Select(tc => (int?)tc.Status)
                .FirstOrDefaultAsync();
            if (completion == StatusHelper.TaskCompleteCompleted)
            {
                var totalCompleted = await _db.TaskCompletes
                    .CountAsync(tc => tc.TaskId == taskId && tc.Status == StatusHelper.TaskCompleteCompleted);
                userStatus = totalCompleted >= task.Quantity ? "Completed" : "Accepted";
            }
            else if (completion.HasValue && completion != StatusHelper.TaskCompleteCancelled)
                userStatus = "Accepted";
            else if (!completion.HasValue && await _db.AcceptedTasks.AnyAsync(a => a.UserId == userId.Value && a.TaskId == taskId))
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
            Status = StatusHelper.TaskGenerateStatusToString(task.Status),
            CreatedAt = task.CreatedAt,
            ExpiresAt = task.CreatedAt.AddDays(3)
        });
    }

    public async Task<Result<AcceptTaskResult>> AcceptTaskAsync(int taskId, int userId)
    {
        var task = await _db.TaskGenerates.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            return Result.Failure<AcceptTaskResult>("Task not found", "NOT_FOUND");
        if (task.Status != StatusHelper.TaskGenerateActive)
            return Result.Failure<AcceptTaskResult>("Task is no longer active", "INACTIVE");

        var worker = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (worker == null || worker.Status != 1)
            return Result.Failure<AcceptTaskResult>("Account is not active", "ACCOUNT_INACTIVE");

        var hasPlatformAccount = await _db.SocialMediaAccounts
            .AnyAsync(s => s.UserId == userId && s.Platform == task.Platform);
        if (!hasPlatformAccount)
            return Result.Failure<AcceptTaskResult>($"You need a {task.Platform} account to accept this task. Please add it in Settings > Social Accounts.", "NO_PLATFORM_ACCOUNT");

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
            Status = StatusHelper.AcceptedTaskAccepted
        });
        await _db.SaveChangesAsync();

        await _activityLog.LogAsync(
            userId: userId, userName: worker?.Name, userEmail: worker?.Email,
            userRole: "User", evt: "TaskAccepted", description: $"User accepted task #{taskId}",
            subjectType: "Task", subjectId: taskId, subjectName: task.Service);

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
            var userAcceptedTaskIds = await _db.AcceptedTasks
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .ToListAsync();

            var userCompletedTaskIds = await _db.TaskCompletes
                .Where(c => c.UserId == userId)
                .Select(c => c.TaskId)
                .ToListAsync();

            var userProofTaskIds = await _db.TaskProofs
                .Where(p => p.UserId == userId)
                .Select(p => p.TaskId)
                .ToListAsync();

            var relatedTaskIds = userAcceptedTaskIds
                .Union(userCompletedTaskIds)
                .Union(userProofTaskIds)
                .Distinct()
                .ToList();

            if (relatedTaskIds.Count == 0)
                return new List<MyTaskDto>();

            var relatedTaskIdSet = relatedTaskIds.ToHashSet();
            var allTasks = await _db.TaskGenerates
                .AsNoTracking()
                .OrderByDescending(t => t.Id)
                .Take(500)
                .ToListAsync();

            var tasks = allTasks.Where(t => relatedTaskIdSet.Contains(t.Id)).Take(100).ToList();

            var taskIds = tasks.Select(t => t.Id).ToList();
            var taskIdSet = taskIds.ToHashSet();

            var proofsList = await _db.TaskProofs
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            var proofs = proofsList
                .Where(p => taskIdSet.Contains(p.TaskId))
                .GroupBy(p => p.TaskId)
                .ToDictionary(g => g.Key, g => g.First());

            var completions = await _db.TaskCompletes
                .Where(tc => tc.UserId == userId)
                .OrderByDescending(tc => tc.Date)
                .Select(tc => new { tc.TaskId, tc.Status, tc.Id })
                .ToListAsync();

            var result = new List<MyTaskDto>();

            foreach (var t in tasks)
            {
                var proof = proofs.GetValueOrDefault(t.Id);
                var comp = completions.FirstOrDefault(c => c.TaskId == t.Id && taskIdSet.Contains(c.TaskId));

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
                        Status = StatusHelper.TaskProofStatusToString(proof.VerificationStatus == StatusHelper.VerificationRejected ? StatusHelper.TaskProofRejected : StatusHelper.TaskProofSubmitted),
                        ProofUrl = proof.ProofUrl,
                        ProofType = proof.ProofType,
                        ProofStatus = StatusHelper.TaskProofStatusToString(proof.Status),
                        VerificationStatus = StatusHelper.VerificationStatusToString(proof.VerificationStatus),
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
                        Status = StatusHelper.TaskCompleteStatusToString(comp.Status),
                        ProofUrl = proof?.ProofUrl,
                        ProofType = proof?.ProofType,
                        ProofStatus = proof != null ? StatusHelper.TaskProofStatusToString(proof.Status) : null,
                        VerificationStatus = proof != null ? StatusHelper.VerificationStatusToString(proof.VerificationStatus) : null,
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
                        Status = StatusHelper.TaskCompleteStatusToString(StatusHelper.TaskCompletePending)
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

            if (task.Status != StatusHelper.TaskGenerateActive)
                return Result.Failure("Task has expired or is no longer available", "TASK_EXPIRED");

            var existingProof = await _db.TaskProofs
                .FirstOrDefaultAsync(p => p.UserId == userId && p.TaskId == taskId && p.VerificationStatus != StatusHelper.VerificationRejected);
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
                Status = StatusHelper.TaskProofSubmitted,
                VerificationStatus = verification.Success ? StatusHelper.VerificationPendingReview : StatusHelper.VerificationRejected,
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

            var proofUser = await _db.Users.FindAsync(userId);
            await _activityLog.LogAsync(
                userId: userId, userName: proofUser?.Name, userEmail: proofUser?.Email,
                userRole: "User", evt: "ProofSubmitted", description: $"Proof submitted for task #{taskId}",
                subjectType: "TaskProof", subjectId: proof.Id, subjectName: task.Service);

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
            .Where(p => p.VerificationStatus == StatusHelper.VerificationPendingReview)
            .Join(_db.Users, p => p.UserId, u => u.Id, (p, u) => new { p, u })
            .Join(_db.TaskGenerates, x => x.p.TaskId, t => t.Id, (x, t) => new { x.p, x.u, t })
            .OrderByDescending(x => x.p.Date)
            .Take(100)
            .Select(x => new ProofReviewDto
            {
                ProofId = x.p.Id,
                TaskId = x.p.TaskId,
                UserId = x.p.UserId,
                UserName = x.u.Name ?? string.Empty,
                ProofUrl = x.p.ProofUrl,
                Platform = x.t.Platform,
                Service = x.t.Service,
                TaskUrl = x.t.Url,
                Reward = x.t.Reward,
                Currency = x.t.Currency,
                SubmittedAt = x.p.Date,
                VerificationStatus = StatusHelper.VerificationStatusToString(x.p.VerificationStatus),
                RejectReason = x.p.RejectReason
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

            if (proof.VerificationStatus != StatusHelper.VerificationPendingReview)
                return Result.Failure("Proof is not pending review", "INVALID_STATUS");

            var alreadyCompleted = await _db.TaskCompletes
                .AnyAsync(tc => tc.TaskId == proof.TaskId && tc.UserId == proof.UserId && tc.Status == StatusHelper.TaskCompleteCompleted);

            if (!alreadyCompleted)
            {
                _db.TaskCompletes.Add(new TaskComplete
                {
                    UserId = proof.UserId,
                    TaskId = proof.TaskId,
                    ProofId = proof.Id,
                    Date = DateTime.UtcNow,
                    Status = StatusHelper.TaskCompleteCompleted
                });
            }

            proof.VerificationStatus = StatusHelper.VerificationApproved;
            proof.Status = StatusHelper.TaskProofCompleted;

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

            var approvedUser = await _db.Users.FindAsync(proof.UserId);
            await _activityLog.LogAsync(
                userId: proof.UserId, userName: approvedUser?.Name, userEmail: approvedUser?.Email,
                userRole: "Admin", evt: "ProofApproved", description: $"Proof #{proofId} approved for task #{proof.TaskId}",
                subjectType: "TaskProof", subjectId: proofId, subjectName: null,
                newValues: JsonSerializer.Serialize(new { VerificationStatus = StatusHelper.VerificationApproved, Reward = proof.Task.Reward }));

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

            if (proof.VerificationStatus != StatusHelper.VerificationPendingReview)
                return Result.Failure("Proof is not pending review", "INVALID_STATUS");

            proof.VerificationStatus = StatusHelper.VerificationRejected;
            proof.RejectReason = reason ?? "Rejected by admin";
            proof.Status = StatusHelper.TaskProofRejected;

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

            var rejectedUser = await _db.Users.FindAsync(proof.UserId);
            await _activityLog.LogAsync(
                userId: proof.UserId, userName: rejectedUser?.Name, userEmail: rejectedUser?.Email,
                userRole: "Admin", evt: "ProofRejected", description: $"Proof #{proofId} rejected for task #{proof.TaskId}: {reason}",
                subjectType: "TaskProof", subjectId: proofId, subjectName: null,
                newValues: JsonSerializer.Serialize(new { VerificationStatus = StatusHelper.VerificationRejected, RejectReason = reason }));

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
            var adminRoleIds = await _db.Roles
                .Where(r => r.RoleTitle == "Admin" || r.RoleTitle == "Administrator")
                .Select(r => r.Id)
                .ToListAsync();

            var allUserRoles = await _db.UserHasRoles
                .Select(ur => new { ur.UserId, ur.RoleId })
                .ToListAsync();

            var adminUserIds = allUserRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToList();

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

    public async Task<PagedResult<MyTaskDto>> GetMyTasksPagedAsync(int userId, MyTaskFilterDto filter)
    {
        try
        {
            var userAcceptedTaskIds = await _db.AcceptedTasks
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .ToListAsync();

            var userCompletedTaskIds = await _db.TaskCompletes
                .Where(c => c.UserId == userId)
                .Select(c => c.TaskId)
                .ToListAsync();

            var userProofTaskIds = await _db.TaskProofs
                .Where(p => p.UserId == userId)
                .Select(p => p.TaskId)
                .ToListAsync();

            var relatedTaskIds = userAcceptedTaskIds
                .Union(userCompletedTaskIds)
                .Union(userProofTaskIds)
                .Distinct()
                .ToList();

            if (relatedTaskIds.Count == 0)
                return new PagedResult<MyTaskDto> { Items = new List<MyTaskDto>(), TotalCount = 0, Page = filter.Page, PageSize = filter.PageSize };

            var relatedTaskIdSet = relatedTaskIds.ToHashSet();

            var query = _db.TaskGenerates
                .AsNoTracking()
                .Where(t => relatedTaskIdSet.Contains(t.Id));

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(t =>
                    (t.Platform != null && t.Platform.ToLower().Contains(search)) ||
                    (t.Service != null && t.Service.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var tasks = await query
                .OrderByDescending(t => t.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var taskIds = tasks.Select(t => t.Id).ToList();
            var taskIdSet = taskIds.ToHashSet();

            var proofsList = await _db.TaskProofs
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            var proofs = proofsList
                .Where(p => taskIdSet.Contains(p.TaskId))
                .GroupBy(p => p.TaskId)
                .ToDictionary(g => g.Key, g => g.First());

            var completions = await _db.TaskCompletes
                .Where(tc => tc.UserId == userId)
                .OrderByDescending(tc => tc.Date)
                .Select(tc => new { tc.TaskId, tc.Status, tc.Id })
                .ToListAsync();

            var result = new List<MyTaskDto>();

            foreach (var t in tasks)
            {
                var proof = proofs.GetValueOrDefault(t.Id);
                var comp = completions.FirstOrDefault(c => c.TaskId == t.Id && taskIdSet.Contains(c.TaskId));

                string status;
                if (proof != null && comp == null)
                    status = StatusHelper.TaskProofStatusToString(proof.VerificationStatus == StatusHelper.VerificationRejected ? StatusHelper.TaskProofRejected : StatusHelper.TaskProofSubmitted);
                else if (comp != null)
                    status = StatusHelper.TaskCompleteStatusToString(comp.Status);
                else
                    status = StatusHelper.TaskCompleteStatusToString(StatusHelper.TaskCompletePending);

                if (!string.IsNullOrEmpty(filter.Status) && !status.Equals(filter.Status, StringComparison.OrdinalIgnoreCase))
                    continue;

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
                        Status = status,
                        ProofUrl = proof.ProofUrl,
                        ProofType = proof.ProofType,
                        ProofStatus = StatusHelper.TaskProofStatusToString(proof.Status),
                        VerificationStatus = StatusHelper.VerificationStatusToString(proof.VerificationStatus),
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
                        Status = status,
                        ProofUrl = proof?.ProofUrl,
                        ProofType = proof?.ProofType,
                        ProofStatus = proof != null ? StatusHelper.TaskProofStatusToString(proof.Status) : null,
                        VerificationStatus = proof != null ? StatusHelper.VerificationStatusToString(proof.VerificationStatus) : null,
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
                        Status = status
                    });
                }
            }

            return new PagedResult<MyTaskDto>
            {
                Items = result,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyTasksPagedAsync failed for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PagedResult<ProofReviewDto>> GetProofsPendingReviewPagedAsync(ProofReviewFilterDto filter)
    {
        var query = _db.TaskProofs
            .AsNoTracking()
            .Where(p => p.VerificationStatus == StatusHelper.VerificationPendingReview)
            .Join(_db.Users, p => p.UserId, u => u.Id, (p, u) => new { p, u })
            .Join(_db.TaskGenerates, x => x.p.TaskId, t => t.Id, (x, t) => new { x.p, x.u, t });

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(x =>
                (x.u.Name != null && x.u.Name.ToLower().Contains(search)) ||
                (x.t.Service != null && x.t.Service.ToLower().Contains(search)));
        }

        if (!string.IsNullOrEmpty(filter.Platform))
            query = query.Where(x => x.t.Platform == filter.Platform);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.p.Date)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new ProofReviewDto
            {
                ProofId = x.p.Id,
                TaskId = x.p.TaskId,
                UserId = x.p.UserId,
                UserName = x.u.Name ?? string.Empty,
                ProofUrl = x.p.ProofUrl,
                Platform = x.t.Platform,
                Service = x.t.Service,
                TaskUrl = x.t.Url,
                Reward = x.t.Reward,
                Currency = x.t.Currency,
                SubmittedAt = x.p.Date,
                VerificationStatus = StatusHelper.VerificationStatusToString(x.p.VerificationStatus),
                RejectReason = x.p.RejectReason
            })
            .ToListAsync();

        return new PagedResult<ProofReviewDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<string>> GetPlatformsAsync()
    {
        return await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == StatusHelper.TaskGenerateActive)
            .Select(t => t.Platform)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetServicesAsync()
    {
        return await _db.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == StatusHelper.TaskGenerateActive)
            .Select(t => t.Service)
            .Distinct()
            .ToListAsync();
    }

    public async Task<Result<List<string>>> GetUserSocialMediaPlatformsAsync(int userId)
    {
        var platforms = await _db.SocialMediaAccounts
            .Where(s => s.UserId == userId)
            .Select(s => s.Platform)
            .ToListAsync();
        return Result.Success(platforms);
    }

    public async Task<int> GetWorkerActiveTaskCountAsync(int userId)
    {
        var acceptedCount = await _db.AcceptedTasks
            .CountAsync(a => a.UserId == userId && a.Status == StatusHelper.AcceptedTaskAccepted);
        var pendingCount = await _db.TaskCompletes
            .CountAsync(tc => tc.UserId == userId && tc.Status == StatusHelper.TaskCompletePending);
        return acceptedCount + pendingCount;
    }
}
