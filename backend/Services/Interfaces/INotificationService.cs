using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsAsync(int userId, bool? isRead = null, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task CreateNotificationAsync(CreateNotificationDto dto);
    Task CreateBulkNotificationAsync(List<CreateNotificationDto> dtos);
}
