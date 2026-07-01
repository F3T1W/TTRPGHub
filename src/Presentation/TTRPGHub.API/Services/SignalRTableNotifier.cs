using Microsoft.AspNetCore.SignalR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Hubs;

namespace TTRPGHub.Services;

internal sealed class SignalRTableNotifier(IHubContext<TableHub> hubContext) : ITableNotifier
{
    public Task NotifyMessageAsync(Guid sessionId, TableMessageDto message, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("TableMessageReceived", message, ct);

    public Task NotifyShowcaseImageChangedAsync(Guid sessionId, string? imageUrl, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("ShowcaseImageChanged", imageUrl, ct);

    public Task NotifyWhisperAsync(Guid senderId, Guid recipientId, TableMessageDto message, CancellationToken ct = default) =>
        hubContext.Clients
            .Users([senderId.ToString(), recipientId.ToString()])
            .SendAsync("TableMessageReceived", message, ct);

    public Task NotifyAudioStateChangedAsync(Guid sessionId, AudioStateDto state, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("AudioStateChanged", state, ct);

    public Task NotifyTokenAddedAsync(Guid sessionId, TableTokenDto token, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("TokenAdded", token, ct);

    public Task NotifyTokenMovedAsync(Guid sessionId, Guid tokenId, double x, double y, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("TokenMoved", tokenId, x, y, ct);

    public Task NotifyTokenRemovedAsync(Guid sessionId, Guid tokenId, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("TokenRemoved", tokenId, ct);
}
