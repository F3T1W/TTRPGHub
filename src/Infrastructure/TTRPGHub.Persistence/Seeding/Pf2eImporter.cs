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
    IPf2eHazardRepository hazardRepo,
    IPf2eVehicleRepository vehicleRepo,
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

        if (!await hazardRepo.AnyAsync(ct))
        {
            logger.LogInformation("Заполняем справочник Pathfinder 2e опасностями (N.1)...");
            await hazardRepo.AddRangeAsync(BuildHazards(), ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (!await vehicleRepo.AnyAsync(ct))
        {
            logger.LogInformation("Заполняем справочник Pathfinder 2e транспортом (N.9)...");
            await vehicleRepo.AddRangeAsync(BuildVehicles(), ct);
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
        string? ResistancesJson, string? WeaknessesJson, string Source, string License,
        string? ImmunitiesJson = null, string? AurasJson = null);

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
            resistancesJson: m.ResistancesJson, weaknessesJson: m.WeaknessesJson,
            immunitiesJson: m.ImmunitiesJson, aurasJson: m.AurasJson));
    }

    // N.1 — в отличие от монстров/заклинаний (английский ORC-дамп + RU-оверлей поверх),
    // у хазардов нет предзагруженного английского набора вообще — источник сразу русский
    // (pf2e-ru-translation, Community Use Policy + OGL, см. /licenses), т.к. своего
    // англоязычного датасета опасностей у нас не было и не появилось.
    private sealed record SeedHazard(
        string Slug, string Name, string NameRu, int Level, string Traits,
        int StealthDc, string? StealthNote, string? Description, string? DisableText,
        int? Ac, int? Fortitude, int? Reflex, int? Hardness, int? Hp,
        string? Immunities, string? AbilitiesText, string? ResetText, string Source);

    private static IEnumerable<Pf2eHazard> BuildHazards()
    {
        var seeds = LoadEmbeddedJson<SeedHazard>("pf2e-hazards.json");
        return seeds.Select(h => Pf2eHazard.Create(
            h.Slug, h.Name, h.NameRu, h.Level, h.Traits,
            h.StealthDc, h.StealthNote, h.Description, h.DisableText,
            h.Ac, h.Fortitude, h.Reflex, h.Hardness, h.Hp,
            h.Immunities, h.AbilitiesText, h.ResetText,
            source: $"{h.Source} (pf2e-ru-translation, Community Use Policy + OGL)"));
    }

    // N.9 — тот же источник и то же обоснование, что у хазардов (N.1): единый файл
    // game_mastering/subsystems/Vehicles.rst в pf2e-ru-translation, своего англоязычного
    // датасета транспорта у нас не было.
    private sealed record SeedVehicle(
        string Slug, string Name, string NameRu, int Level, string? Size, string? Price,
        string? Dimensions, string? Crew, string? Passengers, string? PilotingCheck,
        int? Ac, int? Fortitude, int? Hardness, int? Hp, int? BrokenThreshold,
        string? Immunities, string? Speed, string? Collision, string? AbilitiesText, string Source);

    private static IEnumerable<Pf2eVehicle> BuildVehicles()
    {
        var seeds = LoadEmbeddedJson<SeedVehicle>("pf2e-vehicles.json");
        return seeds.Select(v => Pf2eVehicle.Create(
            v.Slug, v.Name, v.NameRu, v.Level, v.Size, v.Price,
            v.Dimensions, v.Crew, v.Passengers, v.PilotingCheck,
            v.Ac, v.Fortitude, v.Hardness, v.Hp, v.BrokenThreshold,
            v.Immunities, v.Speed, v.Collision, v.AbilitiesText,
            source: $"{v.Source} (pf2e-ru-translation, Community Use Policy + OGL)"));
    }
}
