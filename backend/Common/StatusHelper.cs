namespace BoostingHub.backend.Common;

public static class StatusHelper
{
    // ── User Status ───────────────────────────────────────────────────────────
    // users.status (int) - already stored as int in DB
    public const int UserActive = 1;
    public const int UserInactive = 2;

    public static string UserStatusToString(int status) => status switch
    {
        UserActive => "Active",
        UserInactive => "Inactive",
        _ => "Unknown"
    };

    public static int UserStatusToInt(string status) => status?.ToLower() switch
    {
        "active" => UserActive,
        "inactive" or "locked" => UserInactive,
        _ => UserActive
    };

    // ── Order Status ──────────────────────────────────────────────────────────
    public const int OrderApproved = 1;
    public const int OrderPending = 2;
    public const int OrderRejected = 3;
    public const int OrderCancelled = 4;

    public static string OrderStatusToString(int status) => status switch
    {
        OrderApproved => "Approved",
        OrderPending => "Pending",
        OrderRejected => "Rejected",
        OrderCancelled => "Cancelled",
        _ => "Pending"
    };

    public static int OrderStatusToInt(string status) => status?.ToLower() switch
    {
        "approved" => OrderApproved,
        "pending" => OrderPending,
        "rejected" => OrderRejected,
        "cancelled" => OrderCancelled,
        _ => OrderPending
    };

    // ── Task Generate Status ──────────────────────────────────────────────────
    public const int TaskGenerateActive = 1;
    public const int TaskGenerateExpired = 2;

    public static string TaskGenerateStatusToString(int status) => status switch
    {
        TaskGenerateActive => "Active",
        TaskGenerateExpired => "Expired",
        _ => "Active"
    };

    public static int TaskGenerateStatusToInt(string status) => status?.ToLower() switch
    {
        "active" => TaskGenerateActive,
        "expired" => TaskGenerateExpired,
        _ => TaskGenerateActive
    };

    // ── Task Complete Status ──────────────────────────────────────────────────
    public const int TaskCompleteCompleted = 1;
    public const int TaskCompletePending = 2;
    public const int TaskCompleteCancelled = 3;

    public static string TaskCompleteStatusToString(int status) => status switch
    {
        TaskCompleteCompleted => "Completed",
        TaskCompletePending => "Pending",
        TaskCompleteCancelled => "Cancelled",
        _ => "Pending"
    };

    public static int TaskCompleteStatusToInt(string status) => status?.ToLower() switch
    {
        "completed" => TaskCompleteCompleted,
        "pending" => TaskCompletePending,
        "cancelled" => TaskCompleteCancelled,
        _ => TaskCompletePending
    };

    // ── Task Proof Status ─────────────────────────────────────────────────────
    public const int TaskProofCompleted = 1;
    public const int TaskProofSubmitted = 2;
    public const int TaskProofRejected = 3;

    public static string TaskProofStatusToString(int status) => status switch
    {
        TaskProofCompleted => "Completed",
        TaskProofSubmitted => "Submitted",
        TaskProofRejected => "Rejected",
        _ => "Submitted"
    };

    public static int TaskProofStatusToInt(string status) => status?.ToLower() switch
    {
        "completed" => TaskProofCompleted,
        "submitted" => TaskProofSubmitted,
        "rejected" => TaskProofRejected,
        _ => TaskProofSubmitted
    };

    // ── Task Proof Verification Status ────────────────────────────────────────
    public const int VerificationApproved = 1;
    public const int VerificationPendingReview = 2;
    public const int VerificationRejected = 3;
    public const int VerificationNone = 4;

    public static string VerificationStatusToString(int status) => status switch
    {
        VerificationApproved => "Approved",
        VerificationPendingReview => "PendingReview",
        VerificationRejected => "Rejected",
        VerificationNone => "None",
        _ => "None"
    };

    public static int VerificationStatusToInt(string status) => status?.ToLower() switch
    {
        "approved" => VerificationApproved,
        "pendingreview" or "pending" => VerificationPendingReview,
        "rejected" => VerificationRejected,
        "none" => VerificationNone,
        _ => VerificationNone
    };

    // ── Accepted Task Status ──────────────────────────────────────────────────
    public const int AcceptedTaskAccepted = 1;

    public static string AcceptedTaskStatusToString(int status) => status switch
    {
        AcceptedTaskAccepted => "Accepted",
        _ => "Accepted"
    };

    public static int AcceptedTaskStatusToInt(string status) => status?.ToLower() switch
    {
        "accepted" => AcceptedTaskAccepted,
        _ => AcceptedTaskAccepted
    };

    // ── Wallet Status ─────────────────────────────────────────────────────────
    public const int WalletActive = 1;
    public const int WalletInactive = 2;

    public static string WalletStatusToString(int status) => status switch
    {
        WalletActive => "Active",
        WalletInactive => "Inactive",
        _ => "Active"
    };

    public static int WalletStatusToInt(string status) => status?.ToLower() switch
    {
        "active" => WalletActive,
        "inactive" => WalletInactive,
        _ => WalletActive
    };

    // ── Transaction Status ────────────────────────────────────────────────────
    public const int TransactionCompleted = 1;

    public static string TransactionStatusToString(int status) => status switch
    {
        TransactionCompleted => "Completed",
        _ => "Completed"
    };

    public static int TransactionStatusToInt(string status) => status?.ToLower() switch
    {
        "completed" => TransactionCompleted,
        _ => TransactionCompleted
    };

    // ── Account Status ────────────────────────────────────────────────────────
    public const int AccountActive = 1;
    public const int AccountInactive = 2;

    public static string AccountStatusToString(int status) => status switch
    {
        AccountActive => "Active",
        AccountInactive => "Inactive",
        _ => "Active"
    };

    public static int AccountStatusToInt(string status) => status?.ToLower() switch
    {
        "active" => AccountActive,
        "inactive" => AccountInactive,
        _ => AccountActive
    };
}
