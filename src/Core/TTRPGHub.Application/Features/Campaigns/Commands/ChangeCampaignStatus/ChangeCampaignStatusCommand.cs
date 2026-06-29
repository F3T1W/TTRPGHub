using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Campaigns.Commands.ChangeCampaignStatus;

public sealed record ChangeCampaignStatusCommand(Guid CampaignId, CampaignStatus Status) : IRequest<Result>;
