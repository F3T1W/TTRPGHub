using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;
using TTRPGHub.Translation;

namespace TTRPGHub.Seeding;

// Импортирует категории, для которых в проекте ещё нет отдельных legacy-сущностей
// (классы, расы, фиты, состояния) — сразу в системно-независимую модель RuleEntry.
// Open5e — англоязычный источник, весь текст переводится на русский перед сохранением.
public sealed class Open5eRulesImporter(
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository,
    IUnitOfWork unitOfWork,
    ITranslationService translator,
    ILogger<Open5eRulesImporter> logger,
    HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // "1d12" и подобная игровая нотация ломается машинным переводом (латинская 'd' иногда
    // становится кириллической 'д') — эти поля никогда не переводим.
    private static readonly HashSet<string> DiceNotationKeys = ["hit_dice"];

    public async Task ImportIfEmptyAsync(CancellationToken ct = default)
    {
        var system = await systemRepository.GetBySlugAsync("dnd5e", ct);
        if (system is null)
        {
            logger.LogWarning("Система dnd5e ещё не создана — пропускаю импорт классов/рас/фитов/состояний");
            return;
        }

        if (!await entryRepository.AnyAsync(system.Id, RuleCategory.Class, ct))
            await ImportAsync(system.Id, RuleCategory.Class, "classes", MapClassAsync, ct);

        if (!await entryRepository.AnyAsync(system.Id, RuleCategory.Race, ct))
            await ImportAsync(system.Id, RuleCategory.Race, "races", MapRaceAsync, ct);

        if (!await entryRepository.AnyAsync(system.Id, RuleCategory.Feat, ct))
            await ImportAsync(system.Id, RuleCategory.Feat, "feats", MapFeatAsync, ct);

        if (!await entryRepository.AnyAsync(system.Id, RuleCategory.Condition, ct))
            await ImportAsync(system.Id, RuleCategory.Condition, "conditions", MapConditionAsync, ct);
    }

    private async Task ImportAsync(
        GameSystemId systemId, RuleCategory category, string endpoint,
        Func<GameSystemId, JsonElement, CancellationToken, Task<RuleEntry>> map, CancellationToken ct)
    {
        var url = $"https://api.open5e.com/v1/{endpoint}/?limit=200&document__slug=wotc-srd";
        try
        {
            var response = await http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(response);
            if (!doc.RootElement.TryGetProperty("results", out var results))
                return;

            var entries = new List<RuleEntry>();
            foreach (var el in results.EnumerateArray())
                entries.Add(await map(systemId, el, ct));

            if (entries.Count == 0) return;

            await entryRepository.AddRangeAsync(entries, ct);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Импортировано {Count} записей категории {Category}", entries.Count, category);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка импорта категории {Category} из Open5e", category);
        }
    }

    private async Task<RuleEntry> MapClassAsync(GameSystemId systemId, JsonElement el, CancellationToken ct)
    {
        var name = GetString(el, "name") ?? "";
        var slug = Slugify(el, name);
        var desc = GetString(el, "desc");
        var hd = GetString(el, "hit_dice");

        return RuleEntry.Create(
            systemId, RuleCategory.Class, slug, await translator.TranslateAsync(name, ct),
            summary: hd is not null ? $"Кость хитов: {hd}" : null,
            contentMarkdown: desc is not null ? await translator.TranslateAsync(desc, ct) : null,
            statsJson: await JsonTranslationHelper.TranslateJsonAsync(translator, RawStats(el,
                "hit_dice", "hp_at_1st_level", "hp_at_higher_levels",
                "prof_armor", "prof_weapons", "prof_tools", "prof_saving_throws", "prof_skills",
                "equipment", "table", "spellcasting_ability"), DiceNotationKeys, ct),
            tags: ["класс"], isHomebrew: false, source: "SRD 5.1");
    }

    private async Task<RuleEntry> MapRaceAsync(GameSystemId systemId, JsonElement el, CancellationToken ct)
    {
        var name = GetString(el, "name") ?? "";
        var slug = Slugify(el, name);
        var desc = GetString(el, "desc");
        var sizeRaw = GetString(el, "size_raw");

        return RuleEntry.Create(
            systemId, RuleCategory.Race, slug, await translator.TranslateAsync(name, ct),
            summary: sizeRaw is not null ? await translator.TranslateAsync(sizeRaw, ct) : null,
            contentMarkdown: desc is not null ? await translator.TranslateAsync(desc, ct) : null,
            statsJson: await JsonTranslationHelper.TranslateJsonAsync(translator, RawStats(el,
                "asi", "asi_desc", "age", "alignment", "size", "size_raw",
                "speed", "speed_desc", "languages", "vision", "traits"), ct: ct),
            tags: ["раса"], isHomebrew: false, source: "SRD 5.1");
    }

    private async Task<RuleEntry> MapFeatAsync(GameSystemId systemId, JsonElement el, CancellationToken ct)
    {
        var name = GetString(el, "name") ?? "";
        var slug = Slugify(el, name);
        var desc = GetString(el, "desc");
        var prerequisite = GetString(el, "prerequisite");

        return RuleEntry.Create(
            systemId, RuleCategory.Feat, slug, await translator.TranslateAsync(name, ct),
            summary: prerequisite is not null ? await translator.TranslateAsync(prerequisite, ct) : null,
            contentMarkdown: desc is not null ? await translator.TranslateAsync(desc, ct) : null,
            statsJson: await JsonTranslationHelper.TranslateJsonAsync(translator, RawStats(el, "prerequisite", "effects_desc"), ct: ct),
            tags: ["фит"], isHomebrew: false, source: "SRD 5.1");
    }

    private async Task<RuleEntry> MapConditionAsync(GameSystemId systemId, JsonElement el, CancellationToken ct)
    {
        var name = GetString(el, "name") ?? "";
        var slug = Slugify(el, name);
        var desc = GetString(el, "desc");

        return RuleEntry.Create(
            systemId, RuleCategory.Condition, slug, await translator.TranslateAsync(name, ct),
            summary: null,
            contentMarkdown: desc is not null ? await translator.TranslateAsync(desc, ct) : null,
            statsJson: "{}",
            tags: ["состояние"], isHomebrew: false, source: "SRD 5.1");
    }

    private static string Slugify(JsonElement el, string name) =>
        GetString(el, "slug") ?? name.ToLowerInvariant().Replace(" ", "-");

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string RawStats(JsonElement el, params string[] props)
    {
        var dict = new Dictionary<string, JsonElement>();
        foreach (var prop in props)
            if (el.TryGetProperty(prop, out var value))
                dict[prop] = value.Clone();
        return JsonSerializer.Serialize(dict, JsonOpts);
    }
}
