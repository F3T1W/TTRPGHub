using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? Bio,
    string? City,
    string? AvatarUrl,
    string ExperienceLevel,
    DateTime MemberSince,
    List<PublicCharacterDto> Characters,
    List<PublicCampaignDto> Campaigns);

public sealed record PublicCharacterDto(Guid Id, string Name, string Race, string Class, int Level);

public sealed record PublicCampaignDto(Guid Id, string Title, string System, string Status);
