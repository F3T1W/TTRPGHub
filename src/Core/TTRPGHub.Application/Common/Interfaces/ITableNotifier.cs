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
    Task NotifyTokenUpdatedAsync(Guid sessionId, TableTokenDto token, CancellationToken ct = default);

    // J.7 — в отличие от NotifyTokenUpdatedAsync (шлётся всей группе), видимость токена нельзя
    // разослать всем: игроки, потерявшие доступ, не должны даже увидеть, что токен существовал.
    // Реализация сперва рассылает TokenRemoved всей группе, затем TokenAdded только тем, кому
    // токен действительно виден (Clients.Users, как в NotifyWhisperAsync) — или всей группе,
    // если токен снова стал общедоступным (VisibleToUserIds == null).
    Task NotifyTokenVisibilityChangedAsync(Guid sessionId, Guid organizerId, TableTokenDto token, CancellationToken ct = default);
    Task NotifyGridCellSizeChangedAsync(Guid sessionId, int px, CancellationToken ct = default);
    Task NotifyFogSettingsChangedAsync(Guid sessionId, bool enabled, int visionRadiusFeet, CancellationToken ct = default);
    Task NotifyJournalEntryChangedAsync(Guid sessionId, JournalEntryDto entry, CancellationToken ct = default);
    Task NotifyJournalEntryRemovedAsync(Guid sessionId, Guid entryId, CancellationToken ct = default);
    Task NotifyWallsChangedAsync(Guid sessionId, string? wallsJson, CancellationToken ct = default);
    Task NotifyCombatStateChangedAsync(Guid sessionId, bool active, int round, Guid? turnTokenId, CancellationToken ct = default);
    Task NotifyLightsChangedAsync(Guid sessionId, string? lightsJson, CancellationToken ct = default);
    Task NotifySceneEnvironmentChangedAsync(Guid sessionId, string? terrainTagsJson, string ambientLighting, CancellationToken ct = default);

    // J.4 — любое изменение состава сцен сессии или переключение активной: клиент реагирует
    // повторным вызовом GetTableState (карта/токены/туман/стены/свет/бой у сцен независимы,
    // проще перезапросить всё целиком, чем присылать diff по каждому полю).
    Task NotifyActiveSceneChangedAsync(Guid sessionId, CancellationToken ct = default);
}
