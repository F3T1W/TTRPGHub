using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Dnd5e;
using TTRPGHub.Translation;

namespace TTRPGHub.Seeding;

// Open5e — англоязычный источник, поэтому весь текстовый контент переводится на русский
// перед сохранением (ITranslationService). Slug генерируется из оригинального английского
// имени ДО перевода, чтобы URL оставались стабильными и человекочитаемыми на латинице.
public sealed class Open5eImporter(
    IDnd5eSpellRepository spellRepo,
    IDnd5eMonsterRepository monsterRepo,
    IUnitOfWork unitOfWork,
    ITranslationService translator,
    ILogger<Open5eImporter> logger,
    HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Игровая нотация костей ("1d6") ломается машинным переводом — не переводим такие поля.
    private static readonly HashSet<string> DiceNotationKeys = ["damage_dice"];

    public async Task ImportIfEmptyAsync(CancellationToken ct = default)
    {
        if (!await spellRepo.AnyAsync(ct))
        {
            logger.LogInformation("Импорт и перевод заклинаний D&D 5e из Open5e...");
            await ImportSpellsAsync(ct);
        }

        if (!await monsterRepo.AnyAsync(ct))
        {
            logger.LogInformation("Импорт и перевод монстров D&D 5e из Open5e...");
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

            var spells = new List<Dnd5eSpell>();
            foreach (var s in page.Results)
            {
                var slug = s.Slug ?? s.Name.ToLowerInvariant().Replace(" ", "-");

                spells.Add(Dnd5eSpell.Create(
                    slug:          slug,
                    name:          await translator.TranslateAsync(s.Name, ct),
                    level:         s.LevelInt,
                    school:        await translator.TranslateAsync(s.School ?? "", ct),
                    castingTime:   await translator.TranslateAsync(s.CastingTime ?? "", ct),
                    range:         await translator.TranslateAsync(s.Range ?? "", ct),
                    components:    s.Components ?? "",
                    duration:      await translator.TranslateAsync(s.Duration ?? "", ct),
                    concentration: s.Concentration?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                    ritual:        s.Ritual?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                    description:   await translator.TranslateAsync(s.Desc ?? "", ct),
                    higherLevel:   s.HigherLevel is null ? null : await translator.TranslateAsync(s.HigherLevel, ct),
                    classes:       await translator.TranslateAsync(s.DndClass ?? "", ct),
                    material:      s.Material is null ? null : await translator.TranslateAsync(s.Material, ct),
                    source:        "SRD 5.1"));
            }

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

            var monsters = new List<Dnd5eMonster>();
            foreach (var m in page.Results)
            {
                var slug = m.Slug ?? m.Name.ToLowerInvariant().Replace(" ", "-");

                monsters.Add(Dnd5eMonster.Create(
                    slug:             slug,
                    name:             await translator.TranslateAsync(m.Name, ct),
                    size:             await translator.TranslateAsync(m.Size ?? "", ct),
                    type:             await translator.TranslateAsync(m.Type ?? "", ct),
                    subtype:          string.IsNullOrEmpty(m.Subtype) ? null : await translator.TranslateAsync(m.Subtype, ct),
                    alignment:        await translator.TranslateAsync(m.Alignment ?? "", ct),
                    armorClass:       m.ArmorClass,
                    armorDesc:        m.ArmorDesc is null ? null : await translator.TranslateAsync(m.ArmorDesc, ct),
                    hitPoints:        m.HitPoints,
                    hitDice:          m.HitDice ?? "",
                    speed:            m.Speed != null ? await translator.TranslateAsync(FormatSpeed(m.Speed), ct) : "",
                    str:              m.Strength,
                    dex:              m.Dexterity,
                    con:              m.Constitution,
                    intel:            m.Intelligence,
                    wis:              m.Wisdom,
                    cha:              m.Charisma,
                    challengeRating:  m.ChallengeRating ?? "0",
                    xp:               m.Xp,
                    senses:           m.Senses is null ? null : await translator.TranslateAsync(m.Senses, ct),
                    languages:        m.Languages is null ? null : await translator.TranslateAsync(m.Languages, ct),
                    actions:          m.Actions != null ? await JsonTranslationHelper.TranslateJsonAsync(translator, JsonSerializer.Serialize(m.Actions), DiceNotationKeys, ct) : null,
                    specialAbilities: m.SpecialAbilities != null ? await JsonTranslationHelper.TranslateJsonAsync(translator, JsonSerializer.Serialize(m.SpecialAbilities), DiceNotationKeys, ct) : null,
                    reactions:        m.Reactions != null ? await JsonTranslationHelper.TranslateJsonAsync(translator, JsonSerializer.Serialize(m.Reactions), DiceNotationKeys, ct) : null,
                    legendaryActions: m.LegendaryActions != null ? await JsonTranslationHelper.TranslateJsonAsync(translator, JsonSerializer.Serialize(m.LegendaryActions), DiceNotationKeys, ct) : null,
                    source:           "SRD 5.1"));
            }

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
