using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Users.Queries.GetUserProfile;

internal sealed class GetUserProfileQueryHandler(
    IUserRepository userRepo,
    ICharacterRepository characterRepo,
    ICampaignRepository campaignRepo) : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var userId = new UserId(request.UserId);
        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return Error.NotFound(nameof(User));

        var characters = await characterRepo.GetByOwnerAsync(userId, ct);
        var campaigns = await campaignRepo.GetByOrganizerAsync(userId, ct);

        var dto = new UserProfileDto(
            Id:              user.Id.Value,
            Username:        user.Username,
            DisplayName:     user.Profile.DisplayName,
            Bio:             user.Profile.Bio,
            City:            user.Profile.City,
            AvatarUrl:       user.Profile.AvatarUrl,
            ExperienceLevel: user.Profile.ExperienceLevel.ToString(),
            MemberSince:     user.CreatedAt,
            Characters:      characters
                .Where(c => c.IsPublic)
                .Select(c => new PublicCharacterDto(c.Id.Value, c.Name, c.Race, c.Class, c.Level))
                .ToList(),
            Campaigns: campaigns
                .Select(c => new PublicCampaignDto(c.Id.Value, c.Title, c.System, c.Status.ToString()))
                .ToList());

        return Result<UserProfileDto>.Success(dto);
    }
}
