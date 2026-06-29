using Microsoft.AspNetCore.SignalR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;
using TTRPGHub.Hubs;

namespace TTRPGHub.Services;

internal sealed class SignalRTrackerNotifier(IHubContext<InitiativeHub> hubContext) : ITrackerNotifier
{
    public Task NotifyTrackerUpdatedAsync(Guid trackerId, TrackerDetailDto state, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(InitiativeHub.GroupName(trackerId))
            .SendAsync("TrackerUpdated", state, ct);
}
