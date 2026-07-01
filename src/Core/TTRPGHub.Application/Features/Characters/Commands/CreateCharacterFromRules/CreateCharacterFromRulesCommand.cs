using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;

public sealed record CreateCharacterFromRulesCommand(
    string Name,
    string SystemSlug,
    string RaceSlug,
    string ClassSlug,
    int Level,
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma
) : IRequest<Result<CreateCharacterFromRulesResponse>>;

public sealed record CreateCharacterFromRulesResponse(
    Guid CharacterId, string Name, string Race, string Class, int Level,
    int Strength, int Dexterity, int Constitution, int Intelligence, int Wisdom, int Charisma,
    int MaxHitPoints, int ArmorClass, List<string> SavingThrowProficiencies, string? ProficiencyNotes);
