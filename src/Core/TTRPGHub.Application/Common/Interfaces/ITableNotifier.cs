using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Common.Interfaces;

public interface ITableNotifier
{
    Task NotifyMessageAsync(Guid sessionId, TableMessageDto message, CancellationToken ct = default);
    Task NotifyShowcaseImageChangedAsync(Guid sessionId, string? imageUrl, CancellationToken ct = default);
    Task NotifyWhisperAsync(Guid senderId, Guid recipientId, TableMessageDto message, CancellationToken ct = default);
    Task NotifyAudioStateChangedAsync(Guid sessionId, AudioStateDto state, CancellationToken ct = default);
    Task NotifyTokenAddedAsync(Guid sessionId, TableTokenDto token, CancellationToken ct = default);
    Task NotifyTokenMovedAsync(Guid sessionId, Guid tokenId, double x, double y, CancellationToken ct = default);
    Task NotifyTokenRemovedAsync(Guid sessionId, Guid tokenId, CancellationToken ct = default);
}
