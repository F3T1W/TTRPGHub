using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Calendar.Queries.GetSessionIcs;

public sealed record GetSessionIcsQuery(Guid SessionId, int ReminderMinutes) : IRequest<Result<string>>;
