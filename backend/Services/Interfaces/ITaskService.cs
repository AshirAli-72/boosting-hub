using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface ITaskService
{
    Task<PagedResult<AvailableTaskDto>> GetAvailableTasksAsync(TaskFilterDto filter, int? userId = null);
    Task<Result<TaskDetailDto>> GetTaskDetailAsync(int taskId, int? userId = null);
    Task<Result<AcceptTaskResult>> AcceptTaskAsync(int taskId, int userId);
    Task<List<string>> GetPlatformsAsync();
    Task<List<string>> GetServicesAsync();
    Task<int> GetWorkerActiveTaskCountAsync(int userId);
    Task<List<MyTaskDto>> GetMyTasksAsync(int userId);
    Task<Result> SubmitProofAsync(int taskId, string proofUrl, string proofType, int userId);
}
