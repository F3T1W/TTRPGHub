using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Calendar.Queries.GetSessionIcs;

internal sealed class GetSessionIcsQueryHandler(
    IGameSessionRepository sessionRepository,
    IUserCalendarPreferenceRepository preferenceRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetSessionIcsQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetSessionIcsQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(query.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        // Use user's saved reminder preference if ReminderMinutes not explicitly provided
        int reminder = query.ReminderMinutes;
        if (reminder <= 0 && currentUser.IsAuthenticated)
        {
            var pref = await preferenceRepository.GetByUserIdAsync(currentUser.Id, ct);
            reminder = pref?.ReminderMinutes ?? 60;
        }

        return IcsBuilder.BuildSingleEvent(session, reminder);
    }
}
