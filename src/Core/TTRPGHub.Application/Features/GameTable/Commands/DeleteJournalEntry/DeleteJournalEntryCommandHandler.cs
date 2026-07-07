using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.DeleteJournalEntry;

internal sealed class DeleteJournalEntryCommandHandler(
    IGameSessionRepository sessionRepository,
    IJournalEntryRepository journalRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<DeleteJournalEntryCommand, Result>
{
    public async Task<Result> Handle(DeleteJournalEntryCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var entry = await journalRepository.GetByIdAsync(command.EntryId, ct);
        if (entry is null || entry.SessionId != session.Id)
            return Error.NotFound(nameof(JournalEntry));

        journalRepository.Remove(entry);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyJournalEntryRemovedAsync(command.SessionId, command.EntryId, ct);

        return Result.Success();
    }
}
