using Microsoft.AspNetCore.SignalR;

namespace BoostingHub.backend.Hubs;

public class TaskHub : Hub
{
    public async Task JoinTaskGroup(int taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task-{taskId}");
    }

    public async Task LeaveTaskGroup(int taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task-{taskId}");
    }

    public async Task NotifySlotUpdate(int taskId, int remainingSlots)
    {
        await Clients.Group($"task-{taskId}").SendAsync("SlotUpdated", taskId, remainingSlots);
        await Clients.All.SendAsync("TaskListUpdated");
    }
}
