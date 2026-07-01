using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Seeding;

// Переносит данные из legacy-таблиц (Dnd5eSpell/Monster, Pf2eSpell/Monster) в единую
// системно-независимую модель GameSystem + RuleEntry. Legacy-таблицы и API поверх них
// не удаляются — миграция аддитивна, см. ROADMAP.md "Переходный путь".
public sealed class LegacyRuleMigrator(
    AppDbContext db,
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository,
    IUnitOfWork unitOfWork,
    ILogger<LegacyRuleMigrator> logger)
{
    public async Task MigrateIfEmptyAsync(CancellationToken ct = default)
    {
        var dnd5e = await GetOrCreateSystemAsync("dnd5e", "D&D 5e", ct);
        var pf2e = await GetOrCreateSystemAsync("pf2e", "Pathfinder 2e", ct);

        if (!await entryRepository.AnyAsync(dnd5e.Id, RuleCategory.Spell, ct))
            await MigrateDnd5eSpellsAsync(dnd5e.Id, ct);

        if (!await entryRepository.AnyAsync(dnd5e.Id, RuleCategory.Monster, ct))
            await MigrateDnd5eMonstersAsync(dnd5e.Id, ct);

        if (!await entryRepository.AnyAsync(pf2e.Id, RuleCategory.Spell, ct))
            await MigratePf2eSpellsAsync(pf2e.Id, ct);

        if (!await entryRepository.AnyAsync(pf2e.Id, RuleCategory.Monster, ct))
            await MigratePf2eMonstersAsync(pf2e.Id, ct);
    }

    private async Task<GameSystem> GetOrCreateSystemAsync(string slug, string name, CancellationToken ct)
    {
        var existing = await systemRepository.GetBySlugAsync(slug, ct);
        if (existing is not null) return existing;

        var system = GameSystem.CreateOfficial(slug, name);
        await systemRepository.AddAsync(system, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return system;
    }

    private async Task MigrateDnd5eSpellsAsync(GameSystemId systemId, CancellationToken ct)
    {
        var spells = await db.Set<Entities.Dnd5e.Dnd5eSpell>().AsNoTracking().ToListAsync(ct);
        var entries = spells.Select(s => RuleEntry.Create(
            systemId, RuleCategory.Spell, s.Slug, s.Name,
            summary: $"{LevelLabel(s.Level)} · {s.School}",
            contentMarkdown: s.Description,
            statsJson: JsonSerializer.Serialize(new
            {
                s.Level, s.School, s.CastingTime, s.Range, s.Components, s.Duration,
                s.Concentration, s.Ritual, s.HigherLevel, s.Classes, s.Material
            }),
            tags: s.Classes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            isHomebrew: false, source: s.Source)).ToList();

        await entryRepository.AddRangeAsync(entries, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Мигрировано {Count} заклинаний D&D 5e в RuleEntry", entries.Count);
    }

    private async Task MigrateDnd5eMonstersAsync(GameSystemId systemId, CancellationToken ct)
    {
        var monsters = await db.Set<Entities.Dnd5e.Dnd5eMonster>().AsNoTracking().ToListAsync(ct);
        var entries = monsters.Select(m => RuleEntry.Create(
            systemId, RuleCategory.Monster, m.Slug, m.Name,
            summary: $"{m.Size} {m.Type} · CR {m.ChallengeRating}",
            contentMarkdown: null,
            statsJson: JsonSerializer.Serialize(new
            {
                m.Size, m.Type, m.Subtype, m.Alignment, m.ArmorClass, m.ArmorDesc,
                m.HitPoints, m.HitDice, m.Speed, m.Strength, m.Dexterity, m.Constitution,
                m.Intelligence, m.Wisdom, m.Charisma, m.ChallengeRating, m.Xp,
                Senses = m.SenseStr, Languages = m.LanguagesStr, m.Actions,
                m.SpecialAbilities, m.Reactions, m.LegendaryActions
            }),
            tags: [m.Type],
            isHomebrew: false, source: m.Source)).ToList();

        await entryRepository.AddRangeAsync(entries, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Мигрировано {Count} монстров D&D 5e в RuleEntry", entries.Count);
    }

    private async Task MigratePf2eSpellsAsync(GameSystemId systemId, CancellationToken ct)
    {
        var spells = await db.Set<Entities.Pf2e.Pf2eSpell>().AsNoTracking().ToListAsync(ct);
        var entries = spells.Select(s => RuleEntry.Create(
            systemId, RuleCategory.Spell, s.Slug, s.Name,
            summary: $"{LevelLabel(s.Level)} · {s.Traditions}",
            contentMarkdown: s.Description,
            statsJson: JsonSerializer.Serialize(new
            {
                s.Level, s.Traditions, s.Traits, s.Cast, s.Range, s.Area,
                s.Targets, s.Duration, s.Heightened
            }),
            tags: s.Traits.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            isHomebrew: false, source: s.Source)).ToList();

        await entryRepository.AddRangeAsync(entries, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Мигрировано {Count} заклинаний PF2e в RuleEntry", entries.Count);
    }

    private async Task MigratePf2eMonstersAsync(GameSystemId systemId, CancellationToken ct)
    {
        var monsters = await db.Set<Entities.Pf2e.Pf2eMonster>().AsNoTracking().ToListAsync(ct);
        var entries = monsters.Select(m => RuleEntry.Create(
            systemId, RuleCategory.Monster, m.Slug, m.Name,
            summary: $"{m.Size} · Level {m.Level}",
            contentMarkdown: null,
            statsJson: JsonSerializer.Serialize(new
            {
                m.Level, m.Size, m.Traits, m.Perception, m.Senses, m.Languages, m.Skills,
                m.Strength, m.Dexterity, m.Constitution, m.Intelligence, m.Wisdom, m.Charisma,
                m.ArmorClass, m.Fortitude, m.Reflex, m.Will, m.HitPoints, m.Speed,
                m.Attacks, m.Abilities
            }),
            tags: m.Traits.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            isHomebrew: false, source: m.Source)).ToList();

        await entryRepository.AddRangeAsync(entries, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Мигрировано {Count} монстров PF2e в RuleEntry", entries.Count);
    }

    private static string LevelLabel(int level) => level == 0 ? "Заговор" : $"{level} уровень";
}
