namespace BoostingHub.backend.DTOs;

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int ApprovedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int RejectedOrders { get; set; }
    public decimal AvgOrderValue { get; set; }
    public Dictionary<string, decimal> DailyRevenue { get; set; } = new();
}

public class UsersReportDto
{
    public int TotalUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int UnverifiedUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int JoinedToday { get; set; }
    public Dictionary<string, int> DailyRegistrations { get; set; } = new();
}

public class TasksReportDto
{
    public int TotalTasks { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingProofs { get; set; }
    public int ApprovedProofs { get; set; }
    public int RejectedProofs { get; set; }
    public Dictionary<string, int> DailyCompletions { get; set; } = new();
}

public class OrdersReportDto
{
    public int TotalOrders { get; set; }
    public int ApprovedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int RejectedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public Dictionary<string, int> DailyOrders { get; set; } = new();
}
