using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Calendar.Commands.UpsertCalendarPreference;

public sealed record UpsertCalendarPreferenceCommand(int ReminderMinutes, bool RegenerateToken = false)
    : IRequest<Result<CalendarPreferenceDto>>;

public sealed record CalendarPreferenceDto(Guid CalendarToken, int ReminderMinutes, bool PushEnabled);
