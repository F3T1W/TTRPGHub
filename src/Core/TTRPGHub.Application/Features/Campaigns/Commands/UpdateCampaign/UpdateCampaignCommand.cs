using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Campaigns.Commands.UpdateCampaign;

public sealed record UpdateCampaignCommand(
    Guid CampaignId,
    string Title,
    string? Description,
    string System
) : IRequest<Result>;
