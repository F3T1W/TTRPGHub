using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Initiative.Commands.StartTracker;

public sealed record StartTrackerCommand(Guid TrackerId) : IRequest<Result>;
