using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Calendar.Queries.GetCalendarFeed;

internal sealed class GetCalendarFeedQueryHandler(
    IUserCalendarPreferenceRepository preferenceRepository,
    IGameSessionRepository sessionRepository
) : IRequestHandler<GetCalendarFeedQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetCalendarFeedQuery query, CancellationToken ct)
    {
        var pref = await preferenceRepository.GetByTokenAsync(query.Token, ct);
        if (pref is null)
            return Error.NotFound("CalendarPreference");

        var sessions = await sessionRepository.GetByParticipantAsync(pref.UserId, ct);
        var upcoming = sessions
            .Where(s => s.ScheduledAt >= DateTime.UtcNow.AddDays(-7))
            .OrderBy(s => s.ScheduledAt);

        return IcsBuilder.BuildFeed(upcoming, pref.ReminderMinutes);
    }
}
