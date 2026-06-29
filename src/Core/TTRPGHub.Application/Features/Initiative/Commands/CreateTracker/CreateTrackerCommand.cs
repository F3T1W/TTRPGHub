using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Initiative.Commands.CreateTracker;

public sealed record CreateTrackerCommand(Guid CampaignId, string Name)
    : IRequest<Result<CreateTrackerResponse>>;

public sealed record CreateTrackerResponse(Guid TrackerId);
