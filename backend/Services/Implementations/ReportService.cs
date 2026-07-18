using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<RevenueReportDto> GetRevenueReportAsync()
    {
        var orders = await _db.Orders.AsNoTracking().ToListAsync();

        var totalRevenue   = orders.Where(o => o.Status == StatusHelper.OrderApproved).Sum(o => o.Budget);
        var totalOrders    = orders.Count;
        var approvedOrders = orders.Count(o => o.Status == StatusHelper.OrderApproved);
        var pendingOrders  = orders.Count(o => o.Status == StatusHelper.OrderPending);
        var rejectedOrders = orders.Count(o => o.Status == StatusHelper.OrderRejected);
        var avgOrderValue  = totalOrders > 0 ? orders.Sum(o => o.Budget) / totalOrders : 0;

        var since = DateTime.UtcNow.Date.AddDays(-6);
        var dailyRevenue = new Dictionary<string, decimal>();
        for (var i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            var dayRevenue = orders
                .Where(o => o.Status == StatusHelper.OrderApproved && o.CreatedAt.Date == day)
                .Sum(o => o.Budget);
            dailyRevenue[day.ToString("MMM dd")] = dayRevenue;
        }

        return new RevenueReportDto
        {
            TotalRevenue   = totalRevenue,
            TotalOrders    = totalOrders,
            ApprovedOrders = approvedOrders,
            PendingOrders  = pendingOrders,
            RejectedOrders = rejectedOrders,
            AvgOrderValue  = avgOrderValue,
            DailyRevenue   = dailyRevenue
        };
    }

    public async Task<UsersReportDto> GetUsersReportAsync()
    {
        var users = await _db.Users.AsNoTracking().ToListAsync();
        var today = DateTime.UtcNow.Date;
        var since = today.AddDays(-6);

        var daily = new Dictionary<string, int>();
        for (var i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            var count = users.Count(u => u.CreatedAt.Date == day);
            daily[day.ToString("MMM dd")] = count;
        }

        return new UsersReportDto
        {
            TotalUsers         = users.Count,
            VerifiedUsers      = users.Count(u => u.EmailVerifiedAt != null),
            UnverifiedUsers    = users.Count(u => u.EmailVerifiedAt == null),
            ActiveUsers        = users.Count(u => u.Status == 1),
            LockedUsers        = users.Count(u => u.Status == 0),
            JoinedToday        = users.Count(u => u.CreatedAt.Date == today),
            DailyRegistrations = daily
        };
    }

    public async Task<TasksReportDto> GetTasksReportAsync()
    {
        var totalTasks   = await _db.TaskGenerates.CountAsync();
        var activeTasks  = await _db.TaskGenerates.CountAsync(t => t.Status == StatusHelper.TaskGenerateActive);

        var tasks = await _db.TaskGenerates.ToListAsync();
        var completedCounts = (await _db.TaskCompletes
            .Where(tc => tc.Status == StatusHelper.TaskCompleteCompleted)
            .Select(tc => tc.TaskId)
            .ToListAsync())
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var completedTasks = tasks.Count(t => completedCounts.GetValueOrDefault(t.Id, 0) >= t.Quantity && t.Quantity > 0);
        
        var pendingProofs  = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == StatusHelper.VerificationPendingReview);
        var approvedProofs = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == StatusHelper.VerificationApproved);
        var rejectedProofs = await _db.TaskProofs.CountAsync(p => p.VerificationStatus == StatusHelper.VerificationRejected);

        var since = DateTime.UtcNow.Date.AddDays(-6);
        // Fix SQL CTE error: pull dates into memory, then group in C#
        var completionDates = await _db.TaskCompletes
            .Where(tc => tc.Status == StatusHelper.TaskCompleteCompleted && tc.Date >= since)
            .Select(tc => tc.Date)
            .ToListAsync();

        var raw = completionDates
            .GroupBy(d => d.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        var daily = new Dictionary<string, int>();
        for (var i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            daily[day.ToString("MMM dd")] = raw.FirstOrDefault(r => r.Date == day)?.Count ?? 0;
        }

        return new TasksReportDto
        {
            TotalTasks       = totalTasks,
            ActiveTasks      = activeTasks,
            CompletedTasks   = completedTasks,
            PendingProofs    = pendingProofs,
            ApprovedProofs   = approvedProofs,
            RejectedProofs   = rejectedProofs,
            DailyCompletions = daily
        };
    }
}
