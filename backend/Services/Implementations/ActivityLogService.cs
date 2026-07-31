using System.Text.Json;
using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BoostingHub.backend.Services.Implementations;

public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ActivityLogService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogService(ApplicationDbContext db, ILogger<ActivityLogService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
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
        CancellationToken ct = default)
    {
        try
        {
            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail,
                UserRole = userRole,
                Event = evt,
                Description = description,
                SubjectType = subjectType,
                SubjectId = subjectId,
                SubjectName = subjectName,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _db.ActivityLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write activity log: {Event} on {SubjectType}", evt, subjectType);
        }
    }
}
