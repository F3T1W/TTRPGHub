using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Queries.GetCharacterDetail;

internal sealed class GetCharacterDetailQueryHandler(
    ICharacterRepository characterRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetCharacterDetailQuery, Result<CharacterDetailDto>>
{
    public async Task<Result<CharacterDetailDto>> Handle(GetCharacterDetailQuery query, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(query.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (!character.IsPublic && !character.IsOwnedBy(currentUser.Id))
            return Error.Unauthorized();

        var coOwners = new List<CoOwnerDto>(character.CoOwnerIds.Count);
        foreach (var id in character.CoOwnerIds)
        {
            var user = await userRepository.GetByIdAsync(new UserId(id), ct);
            coOwners.Add(new CoOwnerDto(id, user?.Username ?? "—"));
        }

        return ToDto(character, coOwners);
    }

    private static CharacterDetailDto ToDto(Character c, List<CoOwnerDto> coOwners) => new(
        c.Id.Value, c.OwnerId.Value,
        c.Name, c.Race, c.Class, c.Level, c.IsPublic,
        c.Background, c.Alignment, c.ExperiencePoints,
        c.PersonalityTraits, c.Ideals, c.Bonds, c.Flaws,
        c.Strength, c.Dexterity, c.Constitution, c.Intelligence, c.Wisdom, c.Charisma,
        c.StrengthModifier, c.DexterityModifier, c.ConstitutionModifier,
        c.IntelligenceModifier, c.WisdomModifier, c.CharismaModifier,
        c.ProficiencyBonus, c.Initiative,
        c.MaxHitPoints, c.CurrentHitPoints, c.TemporaryHitPoints,
        c.ArmorClass, c.Speed, c.HitDice,
        c.SkillProficiencies, c.SavingThrowProficiencies,
        c.FeaturesAndTraits, c.Equipment, c.AvatarUrl,
        c.CreatedAt, c.UpdatedAt, c.Pf2eStatsJson, c.SelectedFeatsJson, coOwners
    );
}
