using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.UpdateJournalEntry;

internal sealed class UpdateJournalEntryCommandHandler(
    IGameSessionRepository sessionRepository,
    IJournalEntryRepository journalRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<UpdateJournalEntryCommand, Result>
{
    public async Task<Result> Handle(UpdateJournalEntryCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            return Error.Validation("Journal.Title", "Заголовок обязателен.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var entry = await journalRepository.GetByIdAsync(command.EntryId, ct);
        if (entry is null || entry.SessionId != session.Id)
            return Error.NotFound(nameof(JournalEntry));

        entry.Update(command.Title.Trim(), command.ContentMarkdown, command.ParentId,
            command.CampaignId is { } cid ? new CampaignId(cid) : null);
        journalRepository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);

        // Рассылаем игрокам, только если запись уже опубликована — правки черновика их не касаются.
        if (entry.IsPublished)
            await notifier.NotifyJournalEntryChangedAsync(command.SessionId, JournalEntryMapper.ToDto(entry), ct);

        return Result.Success();
    }
}
