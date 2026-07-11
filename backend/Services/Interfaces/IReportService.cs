using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IReportService
{
    Task<RevenueReportDto> GetRevenueReportAsync();
    Task<UsersReportDto> GetUsersReportAsync();
    Task<TasksReportDto> GetTasksReportAsync();
}
