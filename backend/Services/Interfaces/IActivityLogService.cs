namespace BoostingHub.backend.Services.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(
        int? userId,
        string? userName,
        string? userEmail,
        string userRole,
        string evt,
        string? description,
        string subjectType,
        int? subjectId = null,
        string? subjectName = null,
        string? oldValues = null,
        string? newValues = null,
        HttpContext? httpContext = null,
        CancellationToken ct = default);
}
