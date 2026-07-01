using System.Text.Json;
using System.Text.RegularExpressions;

namespace TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;

// Считает автоматику персонажа (расовые бонусы, HP, AC, спасброски) на основе
// StatsJson записей RuleEntry категорий Race/Class. Данные пришли из Open5e и переведены
// при импорте — короткие структурированные поля (asi, prof_saving_throws) парсим,
// длинные описательные (prof_skills — "Выберите два из...") оставляем как текст-подсказку,
// не пытаясь machine-parse переведённую прозу (см. ROADMAP.md).
internal static partial class CharacterAutomationCalculator
{
    private static readonly Dictionary<string, string> AbilityAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Сила"] = "STR", ["Strength"] = "STR",
        ["Ловкость"] = "DEX", ["Dexterity"] = "DEX",
        // Google Translate непоследовательно переводит "Constitution": иногда как игровой термин
        // "Телосложение", иногда буквально как "Конституция" — учитываем оба варианта.
        ["Телосложение"] = "CON", ["Конституция"] = "CON", ["Constitution"] = "CON",
        ["Интеллект"] = "INT", ["Intelligence"] = "INT",
        ["Мудрость"] = "WIS", ["Wisdom"] = "WIS",
        ["Харизма"] = "CHA", ["Charisma"] = "CHA",
    };

    // [dд] — защита от старых данных, где машинный перевод превратил латинскую 'd' в кириллическую 'д'
    // (сама причина исправлена в Open5eRulesImporter/Open5eImporter — dice-поля больше не переводятся).
    [GeneratedRegex(@"\d*[dд](\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex HitDieRegex();

    public sealed record AbilityScores(int Str, int Dex, int Con, int Int, int Wis, int Cha)
    {
        public AbilityScores Clamp() => new(
            Math.Clamp(Str, 1, 30), Math.Clamp(Dex, 1, 30), Math.Clamp(Con, 1, 30),
            Math.Clamp(Int, 1, 30), Math.Clamp(Wis, 1, 30), Math.Clamp(Cha, 1, 30));
    }

    public sealed record ClassAutomation(string HitDice, int MaxHitPoints, List<string> SavingThrows, string? Equipment, string? ProficiencyNotes);

    public static AbilityScores ApplyRacialBonuses(AbilityScores baseScores, string raceStatsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(raceStatsJson);
            if (!doc.RootElement.TryGetProperty("asi", out var asi) || asi.ValueKind != JsonValueKind.Array)
                return baseScores;

            var (str, dex, con, intl, wis, cha) = (baseScores.Str, baseScores.Dex, baseScores.Con, baseScores.Int, baseScores.Wis, baseScores.Cha);

            foreach (var entry in asi.EnumerateArray())
            {
                if (!entry.TryGetProperty("attributes", out var attrs) || attrs.ValueKind != JsonValueKind.Array)
                    continue;
                if (!entry.TryGetProperty("value", out var valueEl) || !valueEl.TryGetInt32(out var value))
                    continue;

                // Если в записи несколько атрибутов на выбор ("любые два другие") — пропускаем,
                // авто-применение однозначно только для фиксированных бонусов одной характеристике.
                var attributeNames = attrs.EnumerateArray().Select(a => a.GetString() ?? "").ToList();
                if (attributeNames.Count != 1) continue;
                if (!AbilityAliases.TryGetValue(attributeNames[0], out var code)) continue;

                switch (code)
                {
                    case "STR": str += value; break;
                    case "DEX": dex += value; break;
                    case "CON": con += value; break;
                    case "INT": intl += value; break;
                    case "WIS": wis += value; break;
                    case "CHA": cha += value; break;
                }
            }

            return new AbilityScores(str, dex, con, intl, wis, cha).Clamp();
        }
        catch
        {
            return baseScores;
        }
    }

    public static ClassAutomation CalculateClassAutomation(string classStatsJson, int level, int constitutionModifier)
    {
        string? hitDice = null, equipment = null, savingThrowsRaw = null, profSkills = null, profArmor = null, profWeapons = null;

        try
        {
            using var doc = JsonDocument.Parse(classStatsJson);
            hitDice = GetString(doc.RootElement, "hit_dice");
            equipment = GetString(doc.RootElement, "equipment");
            savingThrowsRaw = GetString(doc.RootElement, "prof_saving_throws");
            profSkills = GetString(doc.RootElement, "prof_skills");
            profArmor = GetString(doc.RootElement, "prof_armor");
            profWeapons = GetString(doc.RootElement, "prof_weapons");
        }
        catch { /* используем дефолты ниже */ }

        hitDice ??= "1d8";
        var maxHp = CalculateMaxHitPoints(hitDice, level, constitutionModifier);

        var savingThrows = string.IsNullOrWhiteSpace(savingThrowsRaw)
            ? []
            : savingThrowsRaw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

        var notes = string.Join("\n\n", new[]
        {
            profSkills is not null ? $"Навыки: {profSkills}" : null,
            profArmor is not null ? $"Владение бронёй: {profArmor}" : null,
            profWeapons is not null ? $"Владение оружием: {profWeapons}" : null,
        }.Where(s => s is not null));

        return new ClassAutomation(hitDice, maxHp, savingThrows, equipment, string.IsNullOrEmpty(notes) ? null : notes);
    }

    public static int ParseHitDieMax(string hitDice)
    {
        var match = HitDieRegex().Match(hitDice ?? "1d8");
        return match.Success && int.TryParse(match.Groups[1].Value, out var parsedDie) ? parsedDie : 8;
    }

    public static int CalculateMaxHitPoints(string hitDice, int level, int constitutionModifier)
    {
        var dieMax = ParseHitDieMax(hitDice);

        // 1 уровень: максимум кости + мод. Телосложения. Каждый следующий — среднее значение кости + мод.
        var maxHp = dieMax + constitutionModifier;
        if (level > 1)
            maxHp += (level - 1) * (dieMax / 2 + 1 + constitutionModifier);
        return Math.Max(1, maxHp);
    }

    // Best-effort парсинг markdown-таблицы прогрессии класса (поле "table" в StatsJson) —
    // ищет строку, где первая ячейка начинается с номера искомого уровня, и достаёт колонку
    // с описанием умений ("Features"/переведённые варианты). Если формат не совпал — null,
    // без страницы это не критично, просто не покажем подсказку "что нового".
    public static string? FindLevelFeatures(string classStatsJson, int level)
    {
        try
        {
            using var doc = JsonDocument.Parse(classStatsJson);
            var table = GetString(doc.RootElement, "table");
            if (string.IsNullOrWhiteSpace(table)) return null;

            var rows = table.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(r => r.StartsWith('|'))
                .Select(r => r.Trim('|').Split('|').Select(c => c.Trim()).ToList())
                .Where(cells => cells.Count > 1 && !cells[0].All(ch => ch is '-' or ':'))
                .ToList();

            if (rows.Count < 2) return null;

            var header = rows[0];
            var featuresIdx = header.FindIndex(h =>
                h.Contains("Features", StringComparison.OrdinalIgnoreCase) ||
                h.Contains("Умения", StringComparison.OrdinalIgnoreCase) ||
                h.Contains("Особенности", StringComparison.OrdinalIgnoreCase));
            if (featuresIdx < 0) return null;

            foreach (var row in rows.Skip(1))
            {
                var digitsMatch = LeadingDigitsRegex().Match(row[0]);
                if (digitsMatch.Success && int.TryParse(digitsMatch.Value, out var rowLevel) && rowLevel == level)
                    return featuresIdx < row.Count ? row[featuresIdx] : null;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingDigitsRegex();

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
