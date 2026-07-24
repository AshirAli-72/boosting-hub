using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out var userId))
            return userId;
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var notifications = await _notificationService.GetNotificationsAsync(userId.Value, isRead, page, pageSize);
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);

        return Ok(new { notifications, unreadCount });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(new { count });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        await _notificationService.MarkAsReadAsync(id, userId.Value);
        return Ok(new { message = "Marked as read" });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Not authenticated" });

        await _notificationService.MarkAllAsReadAsync(userId.Value);
        return Ok(new { message = "All marked as read" });
    }
}
