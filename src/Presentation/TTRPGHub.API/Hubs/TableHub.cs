using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TTRPGHub.Hubs;

[Authorize]
public sealed class TableHub : Hub
{
    public async Task JoinTable(string sessionId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"table-{sessionId}");

    public async Task LeaveTable(string sessionId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"table-{sessionId}");

    public static string GroupName(Guid sessionId) => $"table-{sessionId}";
}
