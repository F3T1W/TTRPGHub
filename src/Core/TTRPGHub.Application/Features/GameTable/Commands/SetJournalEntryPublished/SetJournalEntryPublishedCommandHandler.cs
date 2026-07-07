using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetJournalEntryPublished;

internal sealed class SetJournalEntryPublishedCommandHandler(
    IGameSessionRepository sessionRepository,
    IJournalEntryRepository journalRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetJournalEntryPublishedCommand, Result>
{
    public async Task<Result> Handle(SetJournalEntryPublishedCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var entry = await journalRepository.GetByIdAsync(command.EntryId, ct);
        if (entry is null || entry.SessionId != session.Id)
            return Error.NotFound(nameof(JournalEntry));

        entry.SetPublished(command.Published);
        journalRepository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);

        // Публикация — игроки должны сразу увидеть новую запись; снятие с публикации —
        // должны сразу её потерять (используем то же событие "удалена", что и настоящее
        // удаление, с точки зрения игрока разницы нет — запись пропала из его журнала).
        if (command.Published)
            await notifier.NotifyJournalEntryChangedAsync(command.SessionId, JournalEntryMapper.ToDto(entry), ct);
        else
            await notifier.NotifyJournalEntryRemovedAsync(command.SessionId, entry.Id, ct);

        return Result.Success();
    }
}
