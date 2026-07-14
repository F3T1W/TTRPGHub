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
    int Charisma,
    // R.1 — свободный ("ANY") буст предка (человек/полуорк и т.п.) и выбор ключевой характеристики
    // класса, когда их несколько на выбор (Боец/Следопыт STR-или-DEX) — раньше пропускались молча,
    // теперь игрок выбирает их в мастере создания вместо ручной правки после сохранения.
    List<string>? FreeBoostAbilityCodes = null,
    string? KeyAbilityCode = null
) : IRequest<Result<CreateCharacterFromRulesResponse>>;

public sealed record CreateCharacterFromRulesResponse(
    Guid CharacterId, string Name, string Race, string Class, int Level,
    int Strength, int Dexterity, int Constitution, int Intelligence, int Wisdom, int Charisma,
    int MaxHitPoints, int ArmorClass, List<string> SavingThrowProficiencies, string? ProficiencyNotes);
