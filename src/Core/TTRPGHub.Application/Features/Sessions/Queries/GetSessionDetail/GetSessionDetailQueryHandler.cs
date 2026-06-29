using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Queries.GetSessionDetail;

internal sealed class GetSessionDetailQueryHandler(
    IGameSessionRepository repository,
    IUserRepository userRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetSessionDetailQuery, Result<SessionDetailDto>>
{
    public async Task<Result<SessionDetailDto>> Handle(GetSessionDetailQuery query, CancellationToken ct)
    {
        var session = await repository.GetByIdAsync(new GameSessionId(query.SessionId), ct);
        if (session is null) return Error.NotFound(nameof(GameSession));

        var organizer = await userRepository.GetByIdAsync(session.OrganizerId, ct);
        var participants = new List<SessionParticipantDto>();

        foreach (var p in session.Participants)
        {
            var user = await userRepository.GetByIdAsync(p.UserId, ct);
            participants.Add(new SessionParticipantDto(
                p.UserId.Value, user?.Username ?? "—", p.Role, p.JoinedAt));
        }

        return new SessionDetailDto(
            session.Id.Value, session.Title, session.Description, session.System,
            session.MaxPlayers, session.ScheduledAt, session.Status,
            session.OrganizerId.Value, organizer?.Username ?? "—",
            participants,
            session.Participants.Any(p => p.UserId == currentUser.Id),
            session.OrganizerId == currentUser.Id);
    }
}
