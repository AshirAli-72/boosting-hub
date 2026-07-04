namespace BoostingHub.backend.DTOs;

public class AvailableTaskDto
{
    // identifiers
    public int Id { get; set; }          // task_generate id
    public int OrderId { get; set; }    // orders id

    // content
    public string Title { get; set; } = string.Empty;         // derived from Orders.Service
    public string Description { get; set; } = string.Empty;   // from Orders.Description
    public string Platform { get; set; } = string.Empty;       // task_generate.platform (also equals Orders.Platform)
    public string PlatformIcon { get; set; } = string.Empty;  // not present in schema yet
    public string Service { get; set; } = string.Empty;         // task_generate.service
    public string Url { get; set; } = string.Empty;

    // rewards/targets
    public decimal RewardAmount { get; set; }                  // task_generate.reward
    public int TargetQuantity { get; set; }                  // task_generate.quantity
    public int CompletedQuantity { get; set; }              // computed from task_complete

    // Backwards-compatible aliases (so old UI code expecting Quantity/Reward still compiles)
    public int Quantity { get => TargetQuantity; set => TargetQuantity = value; }
    public decimal Reward { get => RewardAmount; set => RewardAmount = value; }

    // proof / timing
    public bool ProofRequired { get; set; } = false;          // no flag in schema yet
    public DateTime? ExpiresAt { get; set; }                  // derived from Orders.CreatedAt (+3 days)

    // user-specific status
    public string UserStatus { get; set; } = "Not Accepted";  // "Not Accepted", "Accepted", "Completed"

    // misc
    public string SocialMediaUrl { get; set; } = string.Empty; // from Orders.SocialMediaUrl
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }                   // task_generate.created_at
}

public class TaskDetailDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;
    public string PlatformIcon { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public decimal RewardAmount { get; set; }
    public int TargetQuantity { get; set; }
    public int CompletedQuantity { get; set; }

    // Backwards-compatible aliases (so old UI code expecting Quantity/Reward still compiles)
    public int Quantity { get => TargetQuantity; set => TargetQuantity = value; }
    public decimal Reward { get => RewardAmount; set => RewardAmount = value; }

    public bool ProofRequired { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public string SocialMediaUrl { get; set; } = string.Empty;

    public string UserStatus { get; set; } = "Not Accepted";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

public class TaskFilterDto
{
    public string? Search { get; set; }
    public string? Platform { get; set; }
    public string? Service { get; set; }
    public decimal? MinReward { get; set; }
    public decimal? MaxReward { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class AcceptTaskRequest
{
    public int TaskId { get; set; }
}

public class AcceptTaskResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TaskCompleteId { get; set; }
}

public class TaskStatisticsDto
{
    public int TotalAvailable { get; set; }
    public int NewToday { get; set; }
    public int EndingSoon { get; set; }
    public int TotalPlatforms { get; set; }
    public decimal HighestReward { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
}

public class LogActivityDto
{
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public string? IpAddress { get; set; }
    public string Level { get; set; } = "Info";
}

public class MyTaskDto
{
    public int TaskCompleteId { get; set; }
    public int TaskId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public decimal Reward { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
    public string? ProofUrl { get; set; }
    public string? ProofType { get; set; }
    public string? ProofStatus { get; set; }
}

public class SubmitProofDto
{
    public string ProofUrl { get; set; } = string.Empty;
    public string ProofType { get; set; } = string.Empty;
}

public class SubmitOrderDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SocialMediaUrl { get; set; }
    public decimal Budget { get; set; }
    public string? Description { get; set; }
}
