using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Features.Calendar.Commands.UpsertCalendarPreference;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Calendar.Queries.GetCalendarPreference;

internal sealed class GetCalendarPreferenceQueryHandler(
    IUserCalendarPreferenceRepository repository,
    ICurrentUser currentUser
) : IRequestHandler<GetCalendarPreferenceQuery, Result<CalendarPreferenceDto>>
{
    public async Task<Result<CalendarPreferenceDto>> Handle(GetCalendarPreferenceQuery query, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Unauthorized();

        var pref = await repository.GetByUserIdAsync(currentUser.Id, ct);
        if (pref is null)
            return new CalendarPreferenceDto(Guid.Empty, 60, false);

        return new CalendarPreferenceDto(pref.CalendarToken, pref.ReminderMinutes, pref.PushEnabled);
    }
}
