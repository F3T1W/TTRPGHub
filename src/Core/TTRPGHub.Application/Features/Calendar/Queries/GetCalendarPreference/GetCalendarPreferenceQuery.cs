using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Calendar.Commands.UpsertCalendarPreference;

namespace TTRPGHub.Features.Calendar.Queries.GetCalendarPreference;

public sealed record GetCalendarPreferenceQuery : IRequest<Result<CalendarPreferenceDto>>;
