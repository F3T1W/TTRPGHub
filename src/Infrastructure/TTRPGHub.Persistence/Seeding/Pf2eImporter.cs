using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Seeding;

public sealed class Pf2eImporter(
    IPf2eSpellRepository spellRepo,
    IPf2eMonsterRepository monsterRepo,
    IUnitOfWork unitOfWork,
    ILogger<Pf2eImporter> logger)
{
    public async Task ImportIfEmptyAsync(CancellationToken ct = default)
    {
        if (!await spellRepo.AnyAsync(ct))
        {
            logger.LogInformation("Заполняем справочник Pathfinder 2e заклинаниями...");
            await spellRepo.AddRangeAsync(BuildSpells(), ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (!await monsterRepo.AnyAsync(ct))
        {
            logger.LogInformation("Заполняем справочник Pathfinder 2e бестиарием...");
            await monsterRepo.AddRangeAsync(BuildMonsters(), ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    // Источник — распакованные данные системы pf2e для Foundry VTT (ORC-лицензия Paizo, см.
    // Seeding/Data/README.md), а не ручной перевод: 1144 заклинания на английском вместо
    // 37 вручную переведённых на русский (см. ROADMAP.md I.6). Ручной перевод такого объёма —
    // отдельная растянутая задача, не блокирующая наличие полного официального набора чисел
    // и механик прямо сейчас.
    private sealed record SeedSpell(
        string Slug, string Name, int Level, string Traditions, string Traits, string Cast,
        string? Range, string? Area, string? Targets, string Duration, string Description,
        string? Heightened, string Source);

    private static IEnumerable<Pf2eSpell> BuildSpells()
    {
        var seeds = LoadEmbeddedJson<SeedSpell>("pf2e-spells.json");
        return seeds.Select(s => Pf2eSpell.Create(
            s.Slug, s.Name, s.Level, s.Traditions, s.Traits, s.Cast,
            s.Range, s.Area, s.Targets, s.Duration, s.Description, s.Heightened,
            $"{s.Source} (Foundry pf2e system data, ORC)"));
    }

    private static List<T> LoadEmbeddedJson<T>(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
            return [];

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return [];

        return JsonSerializer.Deserialize<List<T>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
    }

    private sealed record SeedMonster(
        string Slug, string Name, int Level, string Size, string Traits, int Perception,
        string? Senses, string? Languages, string? Skills,
        int Str, int Dex, int Con, int Int, int Wis, int Cha,
        int Ac, int Fort, int Reflex, int Will, int Hp,
        string Speed, string? Abilities, string? AttacksJson,
        string? ResistancesJson, string? WeaknessesJson, string Source, string License);

    private static IEnumerable<Pf2eMonster> BuildMonsters()
    {
        var seeds = LoadEmbeddedJson<SeedMonster>("pf2e-monsters.json");
        return seeds.Select(m => Pf2eMonster.Create(
            m.Slug, m.Name, m.Level, m.Size, m.Traits, m.Perception,
            m.Senses, m.Languages, m.Skills,
            m.Str, m.Dex, m.Con, m.Int, m.Wis, m.Cha,
            m.Ac, m.Fort, m.Reflex, m.Will, m.Hp,
            m.Speed, attacks: null, m.Abilities,
            source: $"{m.Source} (Foundry pf2e system data, {m.License})",
            attacksJson: m.AttacksJson,
            resistancesJson: m.ResistancesJson, weaknessesJson: m.WeaknessesJson));
    }
}
