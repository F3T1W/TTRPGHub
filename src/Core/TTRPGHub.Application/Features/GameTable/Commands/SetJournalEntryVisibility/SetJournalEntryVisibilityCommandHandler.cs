using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetJournalEntryVisibility;

internal sealed class SetJournalEntryVisibilityCommandHandler(
    IGameSessionRepository sessionRepository,
    IJournalEntryRepository journalRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetJournalEntryVisibilityCommand, Result>
{
    public async Task<Result> Handle(SetJournalEntryVisibilityCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var entry = await journalRepository.GetByIdAsync(command.EntryId, ct);
        if (entry is null || entry.SessionId != session.Id)
            return Error.NotFound(nameof(JournalEntry));

        var json = command.VisibleToUserIds is null ? null : JsonSerializer.Serialize(command.VisibleToUserIds);
        entry.SetVisibility(json);
        journalRepository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);

        if (entry.IsPublished)
            await notifier.NotifyJournalEntryChangedAsync(command.SessionId, JournalEntryMapper.ToDto(entry), ct);

        return Result.Success();
    }
}
