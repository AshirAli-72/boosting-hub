using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IDashboardService
{
    Task<UserDashboardDto> GetUserDashboardAsync(int userId);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
}
