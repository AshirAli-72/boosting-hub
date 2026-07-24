namespace BoostingHub.backend.DTOs;

public class ProofVerificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string VerificationStatus { get; set; } = "None";
}

public class ProofReviewDto
{
    public int ProofId { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ProofUrl { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string TaskUrl { get; set; } = string.Empty;
    public decimal Reward { get; set; }
    public string Currency { get; set; } = "PKR";
    public DateTime SubmittedAt { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
}

public class ApproveRejectDto
{
    public int ProofId { get; set; }
    public string? RejectReason { get; set; }
}

public class ProofReviewFilterDto
{
    public string? Search { get; set; }
    public string? Platform { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
