using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Queries.GetUpcomingSessions;

internal sealed class GetUpcomingSessionsQueryHandler(
    IGameSessionRepository repository,
    IUserRepository userRepository
) : IRequestHandler<GetUpcomingSessionsQuery, Result<IReadOnlyList<SessionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<SessionSummaryDto>>> Handle(GetUpcomingSessionsQuery query, CancellationToken ct)
    {
        var sessions = await repository.GetUpcomingAsync(query.Page, query.PageSize, query.Location, query.Format, ct);
        var result = new List<SessionSummaryDto>(sessions.Count);

        foreach (var s in sessions)
        {
            var organizer = await userRepository.GetByIdAsync(s.OrganizerId, ct);
            result.Add(ToDto(s, organizer?.Username ?? "—"));
        }

        return Result<IReadOnlyList<SessionSummaryDto>>.Success(result);
    }

    private static SessionSummaryDto ToDto(GameSession s, string organizerName) => new(
        s.Id.Value, s.Title, s.Description, s.System,
        s.MaxPlayers, s.Participants.Count,
        s.ScheduledAt, s.Format, s.Location, s.Status,
        s.OrganizerId.Value, organizerName);
}
