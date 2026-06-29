using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.UpdateCharacter;

public sealed record UpdateCharacterCommand(
    Guid CharacterId,
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
    int MaxHitPoints,
    int CurrentHitPoints,
    int TemporaryHitPoints,
    int ArmorClass,
    int Speed,
    string HitDice,
    List<string> SkillProficiencies,
    List<string> SavingThrowProficiencies,
    string? FeaturesAndTraits,
    string? Equipment
) : IRequest<Result>;
