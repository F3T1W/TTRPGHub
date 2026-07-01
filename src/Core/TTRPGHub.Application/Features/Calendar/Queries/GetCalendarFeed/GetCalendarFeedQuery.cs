using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Calendar.Queries.GetCalendarFeed;

public sealed record GetCalendarFeedQuery(Guid Token) : IRequest<Result<string>>;
