using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;

namespace BoostingHub.backend.Repositories.Interfaces;

public interface ITaskRepository : IRepository<Orders>
{
    Task<PagedResult<AvailableTaskDto>> GetAvailableTasksAsync(TaskFilterDto filter, int? userId = null);
    Task<TaskDetailDto?> GetTaskDetailAsync(int taskId);
    Task<TaskStatisticsDto> GetTaskStatisticsAsync();
    // Removed TaskPlatformDto feature references to fix build.
}

