using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(int userId, bool? isRead = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null) return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await _db.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(CreateNotificationDto dto)
    {
        try
        {
            _db.Notifications.Add(new Notification
            {
                UserId = dto.UserId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                Data = dto.Data,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create notification for user {UserId}", dto.UserId);
        }
    }

    public async Task CreateBulkNotificationAsync(List<CreateNotificationDto> dtos)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var dto in dtos)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = dto.UserId,
                    Type = dto.Type,
                    Title = dto.Title,
                    Message = dto.Message,
                    Data = dto.Data,
                    CreatedAt = now
                });
            }
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create bulk notifications");
        }
    }
}
