using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Sessions.Queries.GetUpcomingSessions;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Queries.GetMySessions;

internal sealed class GetMySessionsQueryHandler(
    IGameSessionRepository repository,
    IUserRepository userRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetMySessionsQuery, Result<IReadOnlyList<SessionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<SessionSummaryDto>>> Handle(GetMySessionsQuery query, CancellationToken ct)
    {
        var asMember = await repository.GetByParticipantAsync(currentUser.Id, ct);
        var asOrganizer = await repository.GetByOrganizerAsync(currentUser.Id, ct);

        var all = asMember.Union(asOrganizer, new SessionIdComparer())
            .OrderBy(s => s.ScheduledAt)
            .ToList();

        var result = new List<SessionSummaryDto>(all.Count);
        foreach (var s in all)
        {
            var organizer = await userRepository.GetByIdAsync(s.OrganizerId, ct);
            result.Add(new SessionSummaryDto(
                s.Id.Value, s.Title, s.Description, s.System,
                s.MaxPlayers, s.Participants.Count,
                s.ScheduledAt, s.Format, s.Location, s.Status,
                s.OrganizerId.Value, organizer?.Username ?? "—"));
        }

        return Result<IReadOnlyList<SessionSummaryDto>>.Success(result);
    }

    private sealed class SessionIdComparer : IEqualityComparer<GameSession>
    {
        public bool Equals(GameSession? x, GameSession? y) => x?.Id == y?.Id;
        public int GetHashCode(GameSession obj) => obj.Id.GetHashCode();
    }
}
