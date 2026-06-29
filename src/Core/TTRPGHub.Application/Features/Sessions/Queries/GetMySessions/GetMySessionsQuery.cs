using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Sessions.Queries.GetUpcomingSessions;

namespace TTRPGHub.Features.Sessions.Queries.GetMySessions;

public sealed record GetMySessionsQuery : IRequest<Result<IReadOnlyList<SessionSummaryDto>>>;
