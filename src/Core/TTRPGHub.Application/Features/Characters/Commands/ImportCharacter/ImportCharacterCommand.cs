using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.ImportCharacter;

public sealed record ImportCharacterCommand(
    string Name,
    string Race,
    string Class,
    int Level,
    bool IsPublic = false,
    string? Background = null,
    string? Alignment = null,
    int ExperiencePoints = 0,
    string? PersonalityTraits = null,
    string? Ideals = null,
    string? Bonds = null,
    string? Flaws = null,
    int Strength = 10,
    int Dexterity = 10,
    int Constitution = 10,
    int Intelligence = 10,
    int Wisdom = 10,
    int Charisma = 10,
    int MaxHitPoints = 1,
    int CurrentHitPoints = 1,
    int TemporaryHitPoints = 0,
    int ArmorClass = 10,
    int Speed = 30,
    string HitDice = "1d8",
    List<string>? SkillProficiencies = null,
    List<string>? SavingThrowProficiencies = null,
    string? FeaturesAndTraits = null,
    string? Equipment = null
) : IRequest<Result<ImportCharacterResponse>>;

public sealed record ImportCharacterResponse(Guid CharacterId, string Name);
