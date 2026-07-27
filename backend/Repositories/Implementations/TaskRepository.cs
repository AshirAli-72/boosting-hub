using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Repositories.Implementations;

public class TaskRepository : Repository<Orders>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PagedResult<AvailableTaskDto>> GetAvailableTasksAsync(TaskFilterDto filter, int? userId = null)
    {
        var today = DateTime.UtcNow;

        var allTasks = await _context.TaskGenerates
            .AsNoTracking()
            .Include(t => t.Order)
            .Where(t => t.Status == StatusHelper.TaskGenerateActive)
            .ToListAsync();

        var taskIds = allTasks.Select(t => t.Id).ToList();
        var completedCounts = await _context.TaskCompletes
            .Where(tc => taskIds.Contains(tc.TaskId) && tc.Status == StatusHelper.TaskCompleteCompleted)
            .GroupBy(tc => tc.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.TaskId, g => g.Count);

        var projected = allTasks.Select(t => new AvailableTaskDto
        {
            Id = t.Id,
            OrderId = t.OrderId,
            Title = t.Order?.Service ?? string.Empty,
            Description = t.Order?.Description ?? string.Empty,
            SocialMediaUrl = t.Order?.SocialMediaUrl ?? string.Empty,
            Platform = t.Platform,
            PlatformIcon = string.Empty,
            Service = t.Service,
            Url = t.Url,
            RewardAmount = t.Reward,
            TargetQuantity = t.Quantity,
            CompletedQuantity = completedCounts.GetValueOrDefault(t.Id, 0),
            ProofRequired = false,
            ExpiresAt = t.ExpiryDate,
            Status = StatusHelper.TaskGenerateStatusToString(t.Status),
            CreatedAt = t.CreatedAt
        }).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            projected = projected.Where(x =>
                (x.Title ?? string.Empty).ToLower().Contains(search) ||
                (x.Description ?? string.Empty).ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Platform))
            projected = projected.Where(x => x.Platform == filter.Platform);

        if (!string.IsNullOrWhiteSpace(filter.Service))
            projected = projected.Where(x => x.Service == filter.Service);

        if (filter.MinReward.HasValue)
            projected = projected.Where(x => x.RewardAmount >= filter.MinReward.Value);

        if (filter.MaxReward.HasValue)
            projected = projected.Where(x => x.RewardAmount <= filter.MaxReward.Value);

        projected = projected.Where(x =>
            x.ExpiresAt.HasValue &&
            x.ExpiresAt.Value >= today &&
            x.TargetQuantity > x.CompletedQuantity);

        var filtered = projected.ToList();
        var totalCount = filtered.Count;

        var sorted = (filter.SortBy ?? "newest").ToLower() switch
        {
            "reward" => filtered.OrderByDescending(x => x.RewardAmount),
            "ending" => filtered.OrderBy(x => x.ExpiresAt),
            "popular" => filtered.OrderByDescending(x => x.CompletedQuantity),
            _ => filtered.OrderByDescending(x => x.CreatedAt)
        };

        var items = sorted
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new PagedResult<AvailableTaskDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<TaskDetailDto?> GetTaskDetailAsync(int taskId)
    {
        var today = DateTime.UtcNow;

        var task = await _context.TaskGenerates
            .AsNoTracking()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return null;

        var completedQuantity = await _context.TaskCompletes
            .CountAsync(tc => tc.TaskId == taskId && tc.Status == StatusHelper.TaskCompleteCompleted);

        var dto = new TaskDetailDto
        {
            Id = task.Id,
            OrderId = task.OrderId,
            Title = task.Order?.Service ?? string.Empty,
            Description = task.Order?.Description ?? string.Empty,
            SocialMediaUrl = task.Order?.SocialMediaUrl ?? string.Empty,
            Platform = task.Platform,
            PlatformIcon = string.Empty,
            Service = task.Service,
            Url = task.Url,
            RewardAmount = task.Reward,
            TargetQuantity = task.Quantity,
            CompletedQuantity = completedQuantity,
            ProofRequired = false,
            ExpiresAt = task.ExpiryDate,
            Status = StatusHelper.TaskGenerateStatusToString(task.Status),
            CreatedAt = task.CreatedAt
        };

        if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value < today)
            dto.Status = "Expired";

        return dto;
    }

    public async Task<TaskStatisticsDto> GetTaskStatisticsAsync()
    {
        var today = DateTime.UtcNow;
        var endingSoonDate = today.AddDays(3);

        var allTasks = await _context.TaskGenerates
            .AsNoTracking()
            .Where(t => t.Status == StatusHelper.TaskGenerateActive)
            .ToListAsync();

        var taskIds = allTasks.Select(t => t.Id).ToList();
        var completedCounts = await _context.TaskCompletes
            .Where(tc => taskIds.Contains(tc.TaskId) && tc.Status == StatusHelper.TaskCompleteCompleted)
            .GroupBy(tc => tc.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.TaskId, g => g.Count);

        var totalAvailable = allTasks.Count(t =>
            t.ExpiryDate >= today &&
            t.Quantity > completedCounts.GetValueOrDefault(t.Id, 0));

        var newToday = allTasks.Count(t => t.CreatedAt.Date == today.Date);

        var endingSoon = allTasks.Count(t =>
            t.ExpiryDate <= endingSoonDate && t.ExpiryDate >= today);

        var highest = allTasks.Count > 0 ? allTasks.Max(t => t.Reward) : 0m;

        var totalPlatforms = allTasks.Select(t => t.Platform).Distinct().Count();

        return new TaskStatisticsDto
        {
            TotalAvailable = totalAvailable,
            NewToday = newToday,
            EndingSoon = endingSoon,
            HighestReward = highest,
            TotalPlatforms = totalPlatforms
        };
    }
}

