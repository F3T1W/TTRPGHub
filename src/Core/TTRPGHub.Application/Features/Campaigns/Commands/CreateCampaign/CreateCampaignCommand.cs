using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Campaigns.Commands.CreateCampaign;

public sealed record CreateCampaignCommand(
    string Title,
    string? Description,
    string System
) : IRequest<Result<CreateCampaignResponse>>;

public sealed record CreateCampaignResponse(Guid CampaignId, string Title);
