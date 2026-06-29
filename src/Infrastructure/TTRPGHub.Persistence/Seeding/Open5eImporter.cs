using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Seeding;

public sealed class Open5eImporter(
    IDnd5eSpellRepository spellRepo,
    IDnd5eMonsterRepository monsterRepo,
    IUnitOfWork unitOfWork,
    ILogger<Open5eImporter> logger,
    HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task ImportIfEmptyAsync(CancellationToken ct = default)
    {
        if (!await spellRepo.AnyAsync(ct))
        {
            logger.LogInformation("Импорт заклинаний D&D 5e из Open5e...");
            await ImportSpellsAsync(ct);
        }

        if (!await monsterRepo.AnyAsync(ct))
        {
            logger.LogInformation("Импорт монстров D&D 5e из Open5e...");
            await ImportMonstersAsync(ct);
        }
    }

    private async Task ImportSpellsAsync(CancellationToken ct)
    {
        var url = "https://api.open5e.com/v1/spells/?limit=500&document__slug=wotc-srd";
        try
        {
            var response = await http.GetStringAsync(url, ct);
            var page = JsonSerializer.Deserialize<Open5ePage<Open5eSpell>>(response, JsonOpts);
            if (page?.Results is null) return;

            var spells = page.Results.Select(s => Dnd5eSpell.Create(
                slug:          s.Slug ?? s.Name.ToLowerInvariant().Replace(" ", "-"),
                name:          s.Name,
                level:         s.LevelInt,
                school:        s.School ?? "",
                castingTime:   s.CastingTime ?? "",
                range:         s.Range ?? "",
                components:    s.Components ?? "",
                duration:      s.Duration ?? "",
                concentration: s.Concentration?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                ritual:        s.Ritual?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                description:   s.Desc ?? "",
                higherLevel:   s.HigherLevel,
                classes:       s.DndClass ?? "",
                material:      s.Material,
                source:        "SRD 5.1")).ToList();

            await spellRepo.AddRangeAsync(spells, ct);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Импортировано {Count} заклинаний", spells.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка импорта заклинаний из Open5e");
        }
    }

    private async Task ImportMonstersAsync(CancellationToken ct)
    {
        var url = "https://api.open5e.com/v1/monsters/?limit=500&document__slug=wotc-srd";
        try
        {
            var response = await http.GetStringAsync(url, ct);
            var page = JsonSerializer.Deserialize<Open5ePage<Open5eMonster>>(response, JsonOpts);
            if (page?.Results is null) return;

            var monsters = page.Results.Select(m => Dnd5eMonster.Create(
                slug:             m.Slug ?? m.Name.ToLowerInvariant().Replace(" ", "-"),
                name:             m.Name,
                size:             m.Size ?? "",
                type:             m.Type ?? "",
                subtype:          string.IsNullOrEmpty(m.Subtype) ? null : m.Subtype,
                alignment:        m.Alignment ?? "",
                armorClass:       m.ArmorClass,
                armorDesc:        m.ArmorDesc,
                hitPoints:        m.HitPoints,
                hitDice:          m.HitDice ?? "",
                speed:            m.Speed != null ? FormatSpeed(m.Speed) : "",
                str:              m.Strength,
                dex:              m.Dexterity,
                con:              m.Constitution,
                intel:            m.Intelligence,
                wis:              m.Wisdom,
                cha:              m.Charisma,
                challengeRating:  m.ChallengeRating ?? "0",
                xp:               m.Xp,
                senses:           m.Senses,
                languages:        m.Languages,
                actions:          m.Actions != null ? JsonSerializer.Serialize(m.Actions) : null,
                specialAbilities: m.SpecialAbilities != null ? JsonSerializer.Serialize(m.SpecialAbilities) : null,
                reactions:        m.Reactions != null ? JsonSerializer.Serialize(m.Reactions) : null,
                legendaryActions: m.LegendaryActions != null ? JsonSerializer.Serialize(m.LegendaryActions) : null,
                source:           "SRD 5.1")).ToList();

            await monsterRepo.AddRangeAsync(monsters, ct);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Импортировано {Count} монстров", monsters.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка импорта монстров из Open5e");
        }
    }

    private static string FormatSpeed(Dictionary<string, object>? speed)
    {
        if (speed is null) return "";
        return string.Join(", ", speed
            .Where(kv => kv.Value?.ToString() != "0")
            .Select(kv => kv.Key == "walk" ? $"{kv.Value} ft." : $"{kv.Key} {kv.Value} ft."));
    }

    // ── Open5e DTO models ─────────────────────────────────────────────────────

    private sealed class Open5ePage<T> { public List<T>? Results { get; set; } }

    private sealed class Open5eSpell
    {
        public string? Slug { get; set; }
        public string Name { get; set; } = "";
        [JsonPropertyName("level_int")] public int LevelInt { get; set; }
        public string? School { get; set; }
        [JsonPropertyName("casting_time")] public string? CastingTime { get; set; }
        public string? Range { get; set; }
        public string? Components { get; set; }
        public string? Material { get; set; }
        public string? Duration { get; set; }
        public string? Concentration { get; set; }
        public string? Ritual { get; set; }
        public string? Desc { get; set; }
        [JsonPropertyName("higher_level")] public string? HigherLevel { get; set; }
        [JsonPropertyName("dnd_class")] public string? DndClass { get; set; }
    }

    private sealed class Open5eMonster
    {
        public string? Slug { get; set; }
        public string Name { get; set; } = "";
        public string? Size { get; set; }
        public string? Type { get; set; }
        public string? Subtype { get; set; }
        public string? Alignment { get; set; }
        [JsonPropertyName("armor_class")] public int ArmorClass { get; set; }
        [JsonPropertyName("armor_desc")] public string? ArmorDesc { get; set; }
        [JsonPropertyName("hit_points")] public int HitPoints { get; set; }
        [JsonPropertyName("hit_dice")] public string? HitDice { get; set; }
        public Dictionary<string, object>? Speed { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        [JsonPropertyName("challenge_rating")] public string? ChallengeRating { get; set; }
        public int Xp { get; set; }
        public string? Senses { get; set; }
        public string? Languages { get; set; }
        public List<object>? Actions { get; set; }
        [JsonPropertyName("special_abilities")] public List<object>? SpecialAbilities { get; set; }
        public List<object>? Reactions { get; set; }
        [JsonPropertyName("legendary_actions")] public List<object>? LegendaryActions { get; set; }
    }
}
