using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserDashboardDto> GetUserDashboardAsync(int userId)
    {
        var allActiveTaskIds = await _db.TaskGenerates
            .Where(t => t.Status == StatusHelper.TaskGenerateActive)
            .Select(t => t.Id)
            .ToListAsync();

        var acceptedTaskIds = await _db.AcceptedTasks
            .Where(a => a.UserId == userId)
            .Select(a => a.TaskId)
            .ToListAsync();

        var completedTaskIds = await _db.TaskCompletes
            .Where(tc => tc.UserId == userId && tc.Status == StatusHelper.TaskCompleteCompleted)
            .Select(tc => tc.TaskId)
            .ToListAsync();

        var userTaskIds = acceptedTaskIds.Union(completedTaskIds).ToHashSet();
        var totalAvailable = allActiveTaskIds.Count(id => !userTaskIds.Contains(id));

        var pendingCount = acceptedTaskIds.Count(id => !completedTaskIds.Contains(id));
        var completedCount = completedTaskIds.Count;

        var totalRewards = await _db.TaskCompletes
            .Where(tc => tc.UserId == userId && tc.Status == StatusHelper.TaskCompleteCompleted)
            .Join(_db.TaskGenerates,
                tc => tc.TaskId,
                t => t.Id,
                (tc, t) => t.Reward)
            .SumAsync();

        var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);

        // Fix SQL CTE error: use EF.Functions or pull only dates into memory
        var completionDates = await _db.TaskCompletes
            .Where(tc => tc.UserId == userId && tc.Status == StatusHelper.TaskCompleteCompleted && tc.Date >= sevenDaysAgo)
            .Select(tc => tc.Date)
            .ToListAsync();

        var dailyCompletions = completionDates
            .GroupBy(d => d.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        var lineChart = new ChartDataDto();
        for (var i = 0; i < 7; i++)
        {
            var day = sevenDaysAgo.AddDays(i);
            lineChart.Labels.Add(day.ToString("ddd"));
            var match = dailyCompletions.FirstOrDefault(d => d.Date == day);
            lineChart.Data.Add(match?.Count ?? 0);
        }

        var submittedCount = await _db.TaskProofs
            .CountAsync(p => p.UserId == userId && p.Status == StatusHelper.TaskProofSubmitted);
        var pendingPie = pendingCount - submittedCount;

        var pieChart = new ChartDataDto
        {
            Labels = ["Completed", "Submitted", "In Progress"],
            Data = [completedCount, submittedCount, int.Max(pendingPie, 0)],
            BackgroundColors = ["#10B981", "#F59E0B", "#3B82F6"]
        };

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

        return new UserDashboardDto
        {
            UserName = !string.IsNullOrEmpty(user?.Name) ? user!.Name : user?.Email ?? "User",
            UserEmail = user?.Email ?? "",
            UserStatus = user?.Status == 1 ? "Active" : "Locked",
            TotalTasks = totalAvailable,
            CompletedTasks = completedCount,
            PendingTasks = pendingCount,
            TotalRewards = totalRewards,
            WalletBalance = wallet?.TotalBalance ?? 0,
            WalletStatus = wallet != null ? StatusHelper.WalletStatusToString(wallet.Status) : "Inactive",
            LineChart = lineChart,
            PieChart = pieChart
        };
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        const string seederEmail = "admin@gmail.com";

        // Get admin user IDs efficiently
        var adminRoleIds = await _db.Roles
            .Where(r => r.RoleTitle != null && r.RoleTitle.Contains("Admin"))
            .Select(r => r.Id)
            .ToListAsync();

        var allUserRoles = await _db.UserHasRoles
            .Select(ur => new { ur.UserId, ur.RoleId })
            .ToListAsync();

        var adminUserIds = adminRoleIds.Count > 0
            ? allUserRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToList()
            : new List<int>();

        var today = DateTime.UtcNow.Date;
        var allUsers = await _db.Users
            .Where(u => u.Email != seederEmail)
            .Select(u => new { u.Id, u.Status, u.EmailVerifiedAt, u.CreatedAt })
            .ToListAsync();

        var filteredUsers = allUsers.Where(u => !adminUserIds.Contains(u.Id)).ToList();
        var totalUsers = filteredUsers.Count;
        var locked = filteredUsers.Count(u => u.Status == 0);
        var unverified = filteredUsers.Count(u => u.EmailVerifiedAt == null);
        var registeredToday = filteredUsers.Count(u => u.CreatedAt >= today);

        var totalOrders = await _db.Orders.CountAsync();
        var totalRevenue = await _db.Orders.Where(o => o.Status == StatusHelper.OrderApproved).SumAsync(o => o.Budget);

        // Order chart — only last 7 days
        var sevenDaysAgo = today.AddDays(-6);
        var recentOrderDates = await _db.Orders
            .Where(o => o.CreatedAt >= sevenDaysAgo)
            .Select(o => o.CreatedAt)
            .ToListAsync();

        var dailyOrders = recentOrderDates
            .GroupBy(d => d.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        var lineChart = new ChartDataDto();
        for (var i = 0; i < 7; i++)
        {
            var day = sevenDaysAgo.AddDays(i);
            lineChart.Labels.Add(day.ToString("ddd"));
            var match = dailyOrders.FirstOrDefault(d => d.Date == day);
            lineChart.Data.Add(match?.Count ?? 0);
        }

        // Order status pie chart
        var statusGroups = (await _db.Orders
            .Select(o => StatusHelper.OrderStatusToString(o.Status))
            .ToListAsync())
            .GroupBy(s => s)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        var pieChart = new ChartDataDto();
        var colorMap = new Dictionary<string, string>
        {
            { "Pending", "#F59E0B" },
            { "Approved", "#10B981" },
            { "Rejected", "#EF4444" },
            { "Cancelled", "#6B7280" }
        };
        foreach (var group in statusGroups)
        {
            pieChart.Labels.Add(group.Status);
            pieChart.Data.Add(group.Count);
            pieChart.BackgroundColors.Add(colorMap.GetValueOrDefault(group.Status, "#3B82F6"));
        }

        // Task completion stats
        var completedCounts = (await _db.TaskCompletes
            .Where(tc => tc.Status == StatusHelper.TaskCompleteCompleted)
            .Select(tc => tc.TaskId)
            .ToListAsync())
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var taskStats = await _db.TaskGenerates
            .Select(t => new { t.Id, t.Quantity })
            .ToListAsync();

        var inProgressTasks = taskStats.Count(t => completedCounts.GetValueOrDefault(t.Id, 0) < t.Quantity);
        var completedTasks = taskStats.Count(t => completedCounts.GetValueOrDefault(t.Id, 0) >= t.Quantity && t.Quantity > 0);

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            LockedAccounts = locked,
            UnverifiedEmails = unverified,
            RegisteredToday = registeredToday,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            InProgressTasks = inProgressTasks,
            CompletedTasks = completedTasks,
            LineChart = lineChart,
            PieChart = pieChart
        };
    }
}
