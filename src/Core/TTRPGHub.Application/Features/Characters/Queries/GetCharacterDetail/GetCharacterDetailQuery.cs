using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Queries.GetCharacterDetail;

public sealed record GetCharacterDetailQuery(Guid CharacterId) : IRequest<Result<CharacterDetailDto>>;

public sealed record CharacterDetailDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Race,
    string Class,
    int Level,
    bool IsPublic,
    string? Background,
    string? Alignment,
    int ExperiencePoints,
    string? PersonalityTraits,
    string? Ideals,
    string? Bonds,
    string? Flaws,
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma,
    int StrengthModifier,
    int DexterityModifier,
    int ConstitutionModifier,
    int IntelligenceModifier,
    int WisdomModifier,
    int CharismaModifier,
    int ProficiencyBonus,
    int Initiative,
    int MaxHitPoints,
    int CurrentHitPoints,
    int TemporaryHitPoints,
    int ArmorClass,
    int Speed,
    string HitDice,
    List<string> SkillProficiencies,
    List<string> SavingThrowProficiencies,
    string? FeaturesAndTraits,
    string? Equipment,
    string? AvatarUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
