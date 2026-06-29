using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Campaigns.Queries.GetCampaignDetail;

public sealed record GetCampaignDetailQuery(Guid CampaignId)
    : IRequest<Result<CampaignDetailDto>>;

public sealed record CampaignDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string System,
    CampaignStatus Status,
    Guid OrganizerId,
    string OrganizerName,
    IReadOnlyList<CampaignParticipantDto> Participants,
    bool IsCurrentUserOrganizer,
    bool IsCurrentUserParticipant,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CampaignParticipantDto(
    Guid UserId,
    string Username,
    CampaignRole Role,
    DateTime JoinedAt);
