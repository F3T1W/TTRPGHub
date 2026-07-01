using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Campaigns.Commands.ImportCampaign;

public sealed record ImportCampaignCommand(
    string Title,
    string System,
    string? Description = null
) : IRequest<Result<ImportCampaignResponse>>;

public sealed record ImportCampaignResponse(Guid CampaignId, string Title);
