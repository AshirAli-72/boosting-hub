using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IDashboardService
{
    Task<UserDashboardDto> GetUserDashboardAsync(int userId);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task<PagedResult<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter);
    Task<ActivityLogStatsDto> GetActivityLogStatsAsync();
}
