namespace BoostingHub.backend.DTOs;

public class UserDashboardDto
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public decimal TotalRewards { get; set; }
    public decimal WalletBalance { get; set; }
    public string WalletCurrency { get; set; } = "PKR";
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

public class ActivityLogDto
{
    public int Id { get; set; }
    public string Event { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public int? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Event { get; set; }
    public string? Role { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class ActivityLogStatsDto
{
    public List<ActivityByEventDto> ByEvent { get; set; } = new();
    public List<ActivityByDayDto> ByDay { get; set; } = new();
    public List<ActivityByRoleDto> ByRole { get; set; } = new();
    public int TotalToday { get; set; }
    public int TotalThisWeek { get; set; }
    public int TotalThisMonth { get; set; }
    public string MostActiveUser { get; set; } = string.Empty;
}

public class ActivityByEventDto
{
    public string Event { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActivityByDayDto
{
    public string Day { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActivityByRoleDto
{
    public string Role { get; set; } = string.Empty;
    public int Count { get; set; }
}

