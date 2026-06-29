using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Initiative.Commands.DeleteTracker;

public sealed record DeleteTrackerCommand(Guid TrackerId) : IRequest<Result>;
