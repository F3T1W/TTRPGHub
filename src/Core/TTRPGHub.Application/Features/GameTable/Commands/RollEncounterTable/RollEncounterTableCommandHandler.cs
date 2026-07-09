using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.RollEncounterTable;

// N.12 — бросок таблицы случайных встреч считается на сервере (не на клиенте), иначе GM/игрок
// мог бы подделать результат — тот же принцип, что и у RollDiceCommand (степень успеха
// вычисляется на сервере, клиент только показывает готовый текст).
internal sealed class RollEncounterTableCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableMessageRepository messageRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<RollEncounterTableCommand, Result<TableMessageDto>>
{
    private sealed record EncounterEntry(int Min, int Max, string Label, Guid? MonsterId);
    private sealed record EncounterTable(string Title, List<EncounterEntry> Entries);

    public async Task<Result<TableMessageDto>> Handle(RollEncounterTableCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (!session.IsParticipant(currentUser.Id))
            return Error.Unauthorized();

        EncounterTable? table;
        try
        {
            table = string.IsNullOrWhiteSpace(session.EncounterTableJson)
                ? null
                : JsonSerializer.Deserialize<EncounterTable>(
                    session.EncounterTableJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch { table = null; }

        if (table is null || table.Entries.Count == 0)
            return Error.Validation("EncounterTable.Empty", "У сессии не задана таблица случайных встреч.");

        var dieSize = table.Entries.Max(e => e.Max);
        var roll = Random.Shared.Next(1, dieSize + 1);
        var entry = table.Entries.FirstOrDefault(e => roll >= e.Min && roll <= e.Max);

        var user = await userRepository.GetByIdAsync(currentUser.Id, ct);
        var content = entry is null
            ? $"Таблица встреч «{table.Title}»: {roll} — нет совпадения"
            : entry.MonsterId is { } monsterId
                ? $"Таблица встреч «{table.Title}»: {roll} → {entry.Label} [[monster:{monsterId}]]"
                : $"Таблица встреч «{table.Title}»: {roll} → {entry.Label}";

        var message = TableMessage.CreateRoll(session.Id, currentUser.Id, user?.Username ?? "—", content);
        await messageRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new TableMessageDto(message.Id, message.SenderId.Value, message.SenderUsername, null, null, message.Kind, message.Content, message.CreatedAt);
        await notifier.NotifyMessageAsync(command.SessionId, dto, ct);

        return dto;
    }
}
