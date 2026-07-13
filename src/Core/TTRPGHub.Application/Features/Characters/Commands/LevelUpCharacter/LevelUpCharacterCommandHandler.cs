using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;
using TTRPGHub.Features.Characters.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.LevelUpCharacter;

internal sealed class LevelUpCharacterCommandHandler(
    ICharacterRepository characterRepository,
    ITableTokenRepository tokenRepository,
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICacheService cache
) : IRequestHandler<LevelUpCharacterCommand, Result<LevelUpResponse>>
{
    public async Task<Result<LevelUpResponse>> Handle(LevelUpCharacterCommand command, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (!character.IsOwnedBy(currentUser.Id))
            return Error.Unauthorized();

        if (command.NewLevel <= character.Level || command.NewLevel > 20)
            return Error.Validation("Level.Invalid", "Новый уровень должен быть больше текущего и не превышать 20.");

        var conModifier = character.ConstitutionModifier;
        var newMaxHp = CharacterAutomationCalculator.CalculateMaxHitPoints(character.HitDice, command.NewLevel, conModifier);
        var hpGained = newMaxHp - character.MaxHitPoints;

        // Персонажи, созданные через мастер (см. CreateCharacterFromRules), хранят Class = Title
        // соответствующего RuleEntry — ищем точное совпадение по названию, чтобы достать таблицу
        // прогрессии. Для персонажей со свободным вводом класса совпадения не будет, и это ок —
        // просто не покажем подсказку "что нового", уровень и HP всё равно посчитаются.
        string? whatsNew = null;
        var system = await systemRepository.GetBySlugAsync("dnd5e", ct);
        if (system is not null)
        {
            var candidates = await entryRepository.SearchAsync(system.Id, RuleCategory.Class, character.Class, 1, 10, ct);
            var classEntry = candidates.FirstOrDefault(c => string.Equals(c.Title, character.Class, StringComparison.OrdinalIgnoreCase));
            if (classEntry is not null)
                whatsNew = CharacterAutomationCalculator.FindLevelFeatures(classEntry.StatsJson, command.NewLevel);
        }

        var sheetResult = character.UpdateSheet(new UpdateSheetData(
            Name: character.Name,
            Race: character.Race,
            Class: character.Class,
            Level: command.NewLevel,
            IsPublic: character.IsPublic,
            Background: character.Background,
            Alignment: character.Alignment,
            ExperiencePoints: character.ExperiencePoints,
            PersonalityTraits: character.PersonalityTraits,
            Ideals: character.Ideals,
            Bonds: character.Bonds,
            Flaws: character.Flaws,
            Strength: character.Strength,
            Dexterity: character.Dexterity,
            Constitution: character.Constitution,
            Intelligence: character.Intelligence,
            Wisdom: character.Wisdom,
            Charisma: character.Charisma,
            MaxHitPoints: newMaxHp,
            CurrentHitPoints: Math.Max(0, character.CurrentHitPoints + hpGained),
            TemporaryHitPoints: character.TemporaryHitPoints,
            ArmorClass: character.ArmorClass,
            Speed: character.Speed,
            HitDice: character.HitDice,
            SkillProficiencies: character.SkillProficiencies,
            SavingThrowProficiencies: character.SavingThrowProficiencies,
            FeaturesAndTraits: character.FeaturesAndTraits,
            Equipment: character.Equipment));

        if (sheetResult.IsFailure)
            return sheetResult.Error!;

        characterRepository.Update(character);

        var tokenUpdates = await CharacterTokenSync.SyncTokensAsync(character, tokenRepository, ct);

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var (sessionId, dto) in tokenUpdates)
            await notifier.NotifyTokenUpdatedAsync(sessionId, dto, ct);

        await cache.RemoveAsync($"characters:{command.CharacterId}", ct);
        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);

        return new LevelUpResponse(character.Id.Value, character.Level, character.MaxHitPoints, character.CurrentHitPoints, whatsNew);
    }
}
