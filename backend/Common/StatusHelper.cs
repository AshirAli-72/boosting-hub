namespace BoostingHub.backend.Common;

public static class StatusHelper
{
    // ── User Status ───────────────────────────────────────────────────────────
    // users.status (int) - stored as int in DB
    public const int UserActive = 1;
    public const int UserInactive = 2;

    public static string UserStatusToString(int status) => status switch
    {
        UserActive => "Active",
        UserInactive => "Inactive",
        _ => "Unknown"
    };

    // ── Order Status ──────────────────────────────────────────────────────────
    // orders.status (int) - stored as int in DB
    public const int OrderApproved = 1;
    public const int OrderPending = 2;
    public const int OrderRejected = 3;
    public const int OrderCancelled = 4;

    public static string OrderStatusToString(int status) => status switch
    {
        OrderApproved => "Paid",
        OrderPending => "Pending",
        OrderRejected => "Rejected",
        OrderCancelled => "Cancelled",
        _ => "Pending"
    };

    // ── Task Generate Status ──────────────────────────────────────────────────
    // task_generate.status is nvarchar(50) in DB
    public const string TaskGenerateActive = "1";
    public const string TaskGenerateExpired = "2";

    public static string TaskGenerateStatusToString(string status) => status switch
    {
        "1" => "Active",
        "2" => "Expired",
        _ => "Active"
    };

    // ── Task Complete Status ──────────────────────────────────────────────────
    // task_complete.status is nvarchar(50) in DB
    public const string TaskCompleteCompleted = "1";
    public const string TaskCompletePending = "2";
    public const string TaskCompleteCancelled = "3";

    public static string TaskCompleteStatusToString(string status) => status switch
    {
        "1" => "Completed",
        "2" => "Pending",
        "3" => "Cancelled",
        _ => "Pending"
    };

    // ── Task Proof Status ─────────────────────────────────────────────────────
    // task_proofs.status is nvarchar(50) in DB
    public const string TaskProofCompleted = "1";
    public const string TaskProofSubmitted = "2";
    public const string TaskProofRejected = "3";

    public static string TaskProofStatusToString(string status) => status switch
    {
        "1" => "Completed",
        "2" => "Submitted",
        "3" => "Rejected",
        _ => "Submitted"
    };

    // ── Task Proof Verification Status ────────────────────────────────────────
    // task_proofs.verification_status is nvarchar(50) in DB
    public const string VerificationApproved = "1";
    public const string VerificationPendingReview = "2";
    public const string VerificationRejected = "3";
    public const string VerificationNone = "4";

    public static string VerificationStatusToString(string status) => status switch
    {
        "1" => "Approved",
        "2" => "PendingReview",
        "3" => "Rejected",
        "4" => "None",
        _ => "None"
    };

    // ── Accepted Task Status ──────────────────────────────────────────────────
    // accepted_tasks.status is nvarchar(50) in DB
    public const string AcceptedTaskAccepted = "1";

    public static string AcceptedTaskStatusToString(string status) => status switch
    {
        "1" => "Accepted",
        _ => "Accepted"
    };

    // ── Wallet Status ─────────────────────────────────────────────────────────
    // wallets.status is nvarchar(20) in DB
    public static string WalletStatusToString(string status) => status?.ToLower() switch
    {
        "active" => "Active",
        "inactive" => "Inactive",
        _ => "Active"
    };

    // ── Transaction Status ────────────────────────────────────────────────────
    // transactions.status is nvarchar(50) in DB
    public const string TransactionCompleted = "1";

    public static string TransactionStatusToString(string status) => status switch
    {
        "1" => "Completed",
        _ => "Completed"
    };

    // ── Account Status ────────────────────────────────────────────────────────
    // accounts.status is nvarchar(20) in DB
    public const string AccountActive = "1";
    public const string AccountInactive = "2";

    public static string AccountStatusToString(string status) => status switch
    {
        "1" => "Active",
        "2" => "Inactive",
        _ => "Active"
    };
}
