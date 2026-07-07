using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Queries.GetJournalEntries;

internal sealed class GetJournalEntriesQueryHandler(
    IGameSessionRepository sessionRepository,
    IJournalEntryRepository journalRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetJournalEntriesQuery, Result<List<JournalEntryDto>>>
{
    public async Task<Result<List<JournalEntryDto>>> Handle(GetJournalEntriesQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(query.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (!session.IsParticipant(currentUser.Id))
            return Error.Unauthorized();

        var isOrganizer = session.OrganizerId == currentUser.Id;
        var entries = await journalRepository.GetBySessionAsync(session.Id, ct);

        return entries
            .Where(e => e.IsVisibleTo(isOrganizer, currentUser.Id,
                TableTokenMapper.ParseVisibleTo(e.VisibleToUserIdsJson)))
            .Select(JournalEntryMapper.ToDto)
            .ToList();
    }
}
