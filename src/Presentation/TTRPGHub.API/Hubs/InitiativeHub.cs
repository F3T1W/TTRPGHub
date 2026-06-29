using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TTRPGHub.Hubs;

[Authorize]
public sealed class InitiativeHub : Hub
{
    public async Task JoinTracker(string trackerId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tracker-{trackerId}");

    public async Task LeaveTracker(string trackerId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tracker-{trackerId}");

    public static string GroupName(Guid trackerId) => $"tracker-{trackerId}";
}
