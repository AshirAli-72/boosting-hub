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
        var ordersWithPackages = await _db.Orders
            .AsNoTracking()
            .GroupJoin(_db.Packages, o => o.PackageId, p => p.Id, (o, p) => new { Order = o, Packages = p })
            .SelectMany(x => x.Packages.DefaultIfEmpty(), (x, pkg) => new { x.Order, Package = pkg })
            .ToListAsync();

        var totalRevenue   = ordersWithPackages.Where(x => x.Order.Status == StatusHelper.OrderApproved).Sum(x => x.Package?.Price ?? 0);
        var totalOrders    = ordersWithPackages.Count;
        var approvedOrders = ordersWithPackages.Count(x => x.Order.Status == StatusHelper.OrderApproved);
        var pendingOrders  = ordersWithPackages.Count(x => x.Order.Status == StatusHelper.OrderPending);
        var rejectedOrders = ordersWithPackages.Count(x => x.Order.Status == StatusHelper.OrderRejected);
        var avgOrderValue  = totalOrders > 0 ? ordersWithPackages.Sum(x => x.Package?.Price ?? 0) / totalOrders : 0;

        var since = DateTime.UtcNow.Date.AddDays(-6);
        var dailyRevenue = new Dictionary<string, decimal>();
        for (var i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            var dayRevenue = ordersWithPackages
                .Where(x => x.Order.Status == StatusHelper.OrderApproved && x.Order.CreatedAt.Date == day)
                .Sum(x => x.Package?.Price ?? 0);
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

    public async Task<OrdersReportDto> GetOrdersReportAsync()
    {
        var orders = await _db.Orders.AsNoTracking().ToListAsync();
        var packages = await _db.Packages.AsNoTracking().ToListAsync();
        var pkgMap = packages.ToDictionary(p => p.Id, p => p.Price);

        var totalOrders = orders.Count;
        var approvedOrders = orders.Count(o => o.Status == StatusHelper.OrderApproved);
        var pendingOrders = orders.Count(o => o.Status == 2);
        var rejectedOrders = orders.Count(o => o.Status == StatusHelper.OrderRejected);

        var totalRevenue = orders
            .Where(o => o.Status == StatusHelper.OrderApproved && o.PackageId.HasValue)
            .Sum(o => pkgMap.GetValueOrDefault(o.PackageId!.Value, 0));

        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var since = DateTime.UtcNow.Date.AddDays(-6);
        var dailyOrders = new Dictionary<string, int>();
        for (var i = 0; i < 7; i++)
        {
            var day = since.AddDays(i);
            dailyOrders[day.ToString("MMM dd")] = orders.Count(o => o.CreatedAt.Date == day);
        }

        return new OrdersReportDto
        {
            TotalOrders    = totalOrders,
            ApprovedOrders = approvedOrders,
            PendingOrders  = pendingOrders,
            RejectedOrders = rejectedOrders,
            TotalRevenue   = totalRevenue,
            AvgOrderValue  = avgOrderValue,
            DailyOrders    = dailyOrders
        };
    }
}
