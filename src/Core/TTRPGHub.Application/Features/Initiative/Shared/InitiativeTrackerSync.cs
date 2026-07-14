using System.Text.Json;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Shared;

public sealed record TrackerConditionSnapshot(string Slug, string Name, int? Value);

internal sealed class InitiativeTrackerSync(
    IInitiativeTrackerRepository trackerRepository,
    ITableTokenRepository tokenRepository,
    IGameSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITrackerNotifier notifier,
    ITableNotifier tableNotifier)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PushTokenAsync(TableToken token, CancellationToken ct)
    {
        var trackers = await trackerRepository.GetByLinkedSessionAsync(token.SessionId.Value, ct);
        if (trackers.Count == 0)
            return;

        var snapshot = SerializeConditions(token.Conditions);
        var isPc = token.CombatantType == TokenCombatantType.Character;

        foreach (var tracker in trackers)
        {
            tracker.SyncFromToken(
                token.Id, token.Label, token.Initiative,
                token.CurrentHp, token.MaxHp, token.ArmorClass,
                snapshot, isPc);
            tracker.ReorderEntries();
            trackerRepository.Update(tracker);
        }

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var tracker in trackers)
            await notifier.NotifyTrackerUpdatedAsync(tracker.Id.Value, TrackerMapper.ToDto(tracker), ct);
    }

    // R.1 — обратный синк: правка HP/статуса записи трекера (см. UpdateEntryCommandHandler)
    // отражается на привязанном токене на карте, если он есть. Не трогает произвольные условия
    // токена — только HP и слаг "dead" (Unconscious на столе и так авто-выводится из HP<=0,
    // см. H.5, дублировать его сюда незачем; Dead — единственный статус, который GM выставляет
    // вручную и который нет смысла выводить из HP автоматически). Не вызывает PushTokenAsync
    // в ответ — тот всё равно пересчитает те же данные из токена идемпотентно, а не зациклится,
    // но и добавлять лишний круглый рейс без необходимости незачем.
    public async Task PushEntryToTokenAsync(InitiativeEntry entry, CancellationToken ct)
    {
        if (entry.LinkedTokenId is not { } tokenId) return;

        var token = await tokenRepository.GetByIdAsync(tokenId, ct);
        if (token is null) return;

        token.UpdateHp(entry.CurrentHp);

        const string deadSlug = "dead";
        var hasDeadCondition = token.Conditions.Any(c => c.Slug == deadSlug);
        if (entry.Status == EntryStatus.Dead && !hasDeadCondition)
            token.ApplyCondition(deadSlug, "Мёртв", null);
        else if (entry.Status != EntryStatus.Dead && hasDeadCondition)
            token.RemoveCondition(deadSlug);

        tokenRepository.Update(token);
        await unitOfWork.SaveChangesAsync(ct);
        await tableNotifier.NotifyTokenUpdatedAsync(
            token.SessionId.Value, TableTokenMapper.ToDto(token, canMove: true), ct);
    }

    public async Task<Result<TrackerDetailDto>> SyncFromSessionAsync(
        InitiativeTracker tracker, Guid sessionId, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(sessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        tracker.LinkSession(sessionId);

        var tokens = await tokenRepository.GetBySessionAsync(session.Id, ct);
        foreach (var token in tokens.Where(t => t.Initiative is not null))
        {
            tracker.SyncFromToken(
                token.Id, token.Label, token.Initiative,
                token.CurrentHp, token.MaxHp, token.ArmorClass,
                SerializeConditions(token.Conditions),
                token.CombatantType == TokenCombatantType.Character);
        }

        tracker.ReorderEntries();
        trackerRepository.Update(tracker);
        await unitOfWork.SaveChangesAsync(ct);
        var dto = TrackerMapper.ToDto(tracker);
        await notifier.NotifyTrackerUpdatedAsync(tracker.Id.Value, dto, ct);
        return Result<TrackerDetailDto>.Success(dto);
    }

    public static string? SerializeConditions(IReadOnlyList<TokenCondition> conditions)
    {
        if (conditions.Count == 0)
            return null;

        var snapshots = conditions
            .Select(c => new TrackerConditionSnapshot(c.Slug, c.Name, c.Value))
            .ToList();
        return JsonSerializer.Serialize(snapshots, JsonOptions);
    }

    public static IReadOnlyList<TrackerConditionSnapshot> DeserializeConditions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<TrackerConditionSnapshot>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
