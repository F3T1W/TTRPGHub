using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;

internal sealed class CreateCharacterFromRulesCommandHandler(
    ICharacterRepository characterRepository,
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<CreateCharacterFromRulesCommand, Result<CreateCharacterFromRulesResponse>>
{
    public async Task<Result<CreateCharacterFromRulesResponse>> Handle(CreateCharacterFromRulesCommand command, CancellationToken ct)
    {
        var system = await systemRepository.GetBySlugAsync(command.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        var race = await entryRepository.GetBySlugAsync(system.Id, RuleCategory.Race, command.RaceSlug, ct);
        if (race is null)
            return Error.NotFound("Race");

        var characterClass = await entryRepository.GetBySlugAsync(system.Id, RuleCategory.Class, command.ClassSlug, ct);
        if (characterClass is null)
            return Error.NotFound("Class");

        var baseScores = new CharacterAutomationCalculator.AbilityScores(
            command.Strength, command.Dexterity, command.Constitution,
            command.Intelligence, command.Wisdom, command.Charisma).Clamp();

        var (finalScores, maxHp, armorClass, speed, savingThrows, notes, hitDice, equipment) = command.SystemSlug == "pf2e"
            ? CalculatePf2e(baseScores, race.StatsJson, characterClass.StatsJson, command.Level)
            : CalculateDnd5e(baseScores, race.StatsJson, characterClass.StatsJson, command.Level);

        var characterResult = Character.Create(currentUser.Id, command.Name, race.Title, characterClass.Title, command.Level);
        if (characterResult.IsFailure)
            return characterResult.Error!;

        var character = characterResult.Value!;
        var sheetResult = character.UpdateSheet(new UpdateSheetData(
            Name: command.Name,
            Race: race.Title,
            Class: characterClass.Title,
            Level: command.Level,
            IsPublic: false,
            Background: null,
            Alignment: null,
            ExperiencePoints: 0,
            PersonalityTraits: null,
            Ideals: null,
            Bonds: null,
            Flaws: null,
            Strength: finalScores.Str,
            Dexterity: finalScores.Dex,
            Constitution: finalScores.Con,
            Intelligence: finalScores.Int,
            Wisdom: finalScores.Wis,
            Charisma: finalScores.Cha,
            MaxHitPoints: maxHp,
            CurrentHitPoints: maxHp,
            TemporaryHitPoints: 0,
            ArmorClass: armorClass,
            Speed: speed,
            HitDice: hitDice,
            SkillProficiencies: [],
            SavingThrowProficiencies: savingThrows,
            FeaturesAndTraits: notes,
            Equipment: equipment));

        if (sheetResult.IsFailure)
            return sheetResult.Error!;

        await characterRepository.AddAsync(character, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);

        return new CreateCharacterFromRulesResponse(
            character.Id.Value, character.Name, character.Race, character.Class, character.Level,
            character.Strength, character.Dexterity, character.Constitution,
            character.Intelligence, character.Wisdom, character.Charisma,
            character.MaxHitPoints, character.ArmorClass,
            character.SavingThrowProficiencies, notes);
    }

    private static (CharacterAutomationCalculator.AbilityScores Scores, int MaxHp, int ArmorClass, int Speed,
        List<string> SavingThrows, string? Notes, string HitDice, string? Equipment) CalculateDnd5e(
        CharacterAutomationCalculator.AbilityScores baseScores, string raceStatsJson, string classStatsJson, int level)
    {
        var finalScores = CharacterAutomationCalculator.ApplyRacialBonuses(baseScores, raceStatsJson);
        var conModifier = Modifier(finalScores.Con);
        var dexModifier = Modifier(finalScores.Dex);

        var classAuto = CharacterAutomationCalculator.CalculateClassAutomation(classStatsJson, level, conModifier);
        var armorClass = 10 + dexModifier;

        return (finalScores, classAuto.MaxHitPoints, armorClass, 30, classAuto.SavingThrows, classAuto.ProficiencyNotes, classAuto.HitDice, classAuto.Equipment);
    }

    private static (CharacterAutomationCalculator.AbilityScores Scores, int MaxHp, int ArmorClass, int Speed,
        List<string> SavingThrows, string? Notes, string HitDice, string? Equipment) CalculatePf2e(
        CharacterAutomationCalculator.AbilityScores baseScores, string ancestryStatsJson, string classStatsJson, int level)
    {
        var finalScores = Pf2eCharacterAutomationCalculator.ApplyBoosts(baseScores, ancestryStatsJson, classStatsJson);
        var conModifier = Modifier(finalScores.Con);
        var dexModifier = Modifier(finalScores.Dex);

        var auto = Pf2eCharacterAutomationCalculator.Calculate(ancestryStatsJson, classStatsJson, level, conModifier, dexModifier);

        // PF2e считает спасброски рангами владения (не бинарным "владеет/нет"), Character.SavingThrowProficiencies
        // ожидает список — оставляем пустым, чтобы не подделывать несуществующий D&D5e-стиль владения.
        return (finalScores, auto.MaxHitPoints, auto.ArmorClass, auto.Speed, [], auto.Notes, "—", null);
    }

    private static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);
}
