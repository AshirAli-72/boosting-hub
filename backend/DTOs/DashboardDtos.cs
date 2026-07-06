namespace BoostingHub.backend.DTOs;

public class UserDashboardDto
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public decimal TotalRewards { get; set; }
    public decimal WalletBalance { get; set; }
    public string WalletStatus { get; set; } = "Inactive";
    public ChartDataDto LineChart { get; set; } = new();
    public ChartDataDto PieChart { get; set; } = new();
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserStatus { get; set; } = "Active";
}

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int LockedAccounts { get; set; }
    public int UnverifiedEmails { get; set; }
    public int RegisteredToday { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public ChartDataDto LineChart { get; set; } = new();
    public ChartDataDto PieChart { get; set; } = new();
}

public class ChartDataDto
{
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
    public List<string> BackgroundColors { get; set; } = new();
}
