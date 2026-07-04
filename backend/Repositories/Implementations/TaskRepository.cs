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
        var endingSoonDate = today.AddDays(3);

        // Base query: active task_generate rows
        var baseQuery = _context.TaskGenerates
            .AsNoTracking()
            .Include(t => t.Order)
            .Where(t => t.Status == "Active");

       
        var projected = baseQuery.Select(t => new AvailableTaskDto
        {
            Id = t.Id,
            OrderId = t.OrderId,

            // Derived from Orders
            Title = t.Order.Service ?? string.Empty,
            Description = t.Order.Description ?? string.Empty,
            SocialMediaUrl = t.Order.SocialMediaUrl ?? string.Empty,

            // From TaskGenerate
            Platform = t.Platform,
            PlatformIcon = string.Empty,
            Service = t.Service,
            Url = t.Url,

            RewardAmount = t.Reward,
            TargetQuantity = t.Quantity,

            CompletedQuantity = _context.TaskCompletes.Count(tc =>
                tc.TaskId == t.Id && tc.Status == "Completed"),

            ProofRequired = false,
            ExpiresAt = t.Order.CreatedAt.AddDays(3),

            Status = t.Status,
            CreatedAt = t.CreatedAt
        });

        // Optional filters
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

        // Remaining quantity + not expired
        projected = projected.Where(x =>
            x.ExpiresAt.HasValue &&
            x.ExpiresAt.Value >= today &&
            x.TargetQuantity > x.CompletedQuantity);

        // Total count
        var totalCount = await projected.CountAsync();

        // Sort
        projected = (filter.SortBy ?? "newest").ToLower() switch
        {
            "reward" => projected.OrderByDescending(x => x.RewardAmount),
            "ending" => projected.OrderBy(x => x.ExpiresAt),
            "popular" => projected.OrderByDescending(x => x.CompletedQuantity),
            _ => projected.OrderByDescending(x => x.CreatedAt)
        };

        // Paging
        var items = await projected
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

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

        var dto = await _context.TaskGenerates
            .AsNoTracking()
            .Include(t => t.Order)
            .Where(t => t.Id == taskId)
            .Select(t => new TaskDetailDto
            {
                Id = t.Id,
                OrderId = t.OrderId,

                Title = t.Order.Service ?? string.Empty,
                Description = t.Order.Description ?? string.Empty,
                SocialMediaUrl = t.Order.SocialMediaUrl ?? string.Empty,

                Platform = t.Platform,
                PlatformIcon = string.Empty,
                Service = t.Service,
                Url = t.Url,

                RewardAmount = t.Reward,
                TargetQuantity = t.Quantity,

                CompletedQuantity = _context.TaskCompletes.Count(tc =>
                    tc.TaskId == t.Id && tc.Status == "Completed"),

                ProofRequired = false,
                ExpiresAt = t.Order.CreatedAt.AddDays(3),

                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (dto is null) return null;

        // Optional: keep UI status in sync if expired
        if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value < today)
            dto.Status = "Expired";

        return dto;
    }

    public async Task<TaskStatisticsDto> GetTaskStatisticsAsync()
    {
        var today = DateTime.UtcNow;
        var endingSoonDate = today.AddDays(3);

        var baseQuery = _context.TaskGenerates
            .AsNoTracking()
            .Include(t => t.Order)
            .Where(t => t.Status == "Active");

        // TotalAvailable: tasks not expired and still have remaining slots
        // Using Remaining = quantity > CompletedCount (Completed status)
        // Note: CompletedCount uses TaskComplete.Status == "Completed".
        var projected = baseQuery.Select(t => new
        {
            t.Id,
            t.Quantity,
            Completed = _context.TaskCompletes.Count(tc =>
                tc.TaskId == t.Id && tc.Status == "Completed"),
            ExpiresAt = t.Order.CreatedAt.AddDays(3),
            t.Reward,
            t.CreatedAt,
            t.Platform
        });

        var totalAvailable = await projected.CountAsync(x =>
            x.ExpiresAt >= today && x.Quantity > x.Completed);

        var newToday = await projected.CountAsync(x => x.CreatedAt == today);

        var endingSoon = await projected.CountAsync(x =>
            x.ExpiresAt <= endingSoonDate && x.ExpiresAt >= today);

        var highest = await projected.MaxAsync(x => (decimal?)x.Reward) ?? 0m;

        var totalPlatforms = await baseQuery
            .Select(t => t.Platform)
            .Distinct()
            .CountAsync();

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

