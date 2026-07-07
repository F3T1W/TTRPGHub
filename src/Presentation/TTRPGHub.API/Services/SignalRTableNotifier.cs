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

    public Task NotifyTokenUpdatedAsync(Guid sessionId, TableTokenDto token, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("TokenUpdated", token, ct);

    public async Task NotifyTokenVisibilityChangedAsync(Guid sessionId, Guid organizerId, TableTokenDto token, CancellationToken ct = default)
    {
        await hubContext.Clients.Group(TableHub.GroupName(sessionId)).SendAsync("TokenRemoved", token.Id, ct);

        if (token.VisibleToUserIds is null)
        {
            await hubContext.Clients.Group(TableHub.GroupName(sessionId)).SendAsync("TokenAdded", token, ct);
            return;
        }

        var recipientIds = new HashSet<string> { organizerId.ToString() };
        if (token.OwnerId is { } ownerId) recipientIds.Add(ownerId.ToString());
        foreach (var userId in token.VisibleToUserIds) recipientIds.Add(userId.ToString());

        await hubContext.Clients.Users(recipientIds).SendAsync("TokenAdded", token, ct);
    }

    public Task NotifyGridCellSizeChangedAsync(Guid sessionId, int px, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("GridCellSizeChanged", px, ct);

    public Task NotifyFogSettingsChangedAsync(Guid sessionId, bool enabled, int visionRadiusFeet, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("FogSettingsChanged", enabled, visionRadiusFeet, ct);

    public Task NotifyJournalEntryChangedAsync(Guid sessionId, JournalEntryDto entry, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("JournalEntryChanged", entry, ct);

    public Task NotifyJournalEntryRemovedAsync(Guid sessionId, Guid entryId, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("JournalEntryRemoved", entryId, ct);

    public Task NotifyWallsChangedAsync(Guid sessionId, string? wallsJson, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("WallsChanged", wallsJson, ct);

    public Task NotifyCombatStateChangedAsync(Guid sessionId, bool active, int round, Guid? turnTokenId, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("CombatStateChanged", active, round, turnTokenId, ct);

    public Task NotifyLightsChangedAsync(Guid sessionId, string? lightsJson, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("LightsChanged", lightsJson, ct);

    public Task NotifySceneEnvironmentChangedAsync(Guid sessionId, string? terrainTagsJson, string ambientLighting, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("SceneEnvironmentChanged", terrainTagsJson, ambientLighting, ct);

    public Task NotifyActiveSceneChangedAsync(Guid sessionId, CancellationToken ct = default) =>
        hubContext.Clients
            .Group(TableHub.GroupName(sessionId))
            .SendAsync("ActiveSceneChanged", ct);
}
