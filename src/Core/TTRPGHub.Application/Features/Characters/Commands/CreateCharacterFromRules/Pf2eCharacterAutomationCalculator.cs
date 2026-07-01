using System.Text.Json;

namespace TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;

// Автоматика для Pathfinder 2e — упрощённая, не претендует на полную точность правил:
// - Не моделирует ранги владения (untrained/trained/expert/master/legendary) по отдельным навыкам —
//   Character-сущность хранит один ProficiencyBonus (D&D5e-модель), поэтому AC/спасброски считаются
//   в предположении "тренированное владение" (уровень + 2), без выбора экспертизы на будущих уровнях.
// - Не моделирует доспехи/щиты — AC = 10 + мод. Ловкости + (уровень + 2), как для персонажа без брони.
// - Буст характеристик: только явные пары предка/класса (boost_codes/key_ability_codes) — свободный
//   выбор ("ANY" у человека/полуорка, второй ключевой атрибут у Бойца/Следопыта) не применяется
//   автоматически, игрок сам поднимает нужную характеристику в анкете до отправки формы.
internal static class Pf2eCharacterAutomationCalculator
{
    public sealed record Pf2eAutomation(int MaxHitPoints, int ArmorClass, int Speed, string? Notes);

    public static CharacterAutomationCalculator.AbilityScores ApplyBoosts(
        CharacterAutomationCalculator.AbilityScores baseScores, string ancestryStatsJson, string classStatsJson)
    {
        var scores = baseScores;

        foreach (var code in ReadCodes(ancestryStatsJson, "boost_codes"))
            scores = ApplyBoost(scores, code);

        var flawCode = ReadFlawCode(ancestryStatsJson);
        if (flawCode is not null)
            scores = ApplyFlaw(scores, flawCode);

        var keyAbilityCodes = ReadCodes(classStatsJson, "key_ability_codes");
        if (keyAbilityCodes.Count == 1)
            scores = ApplyBoost(scores, keyAbilityCodes[0]);

        return scores.Clamp();
    }

    public static Pf2eAutomation Calculate(string ancestryStatsJson, string classStatsJson, int level, int conModifier, int dexModifier)
    {
        var ancestryHp = ReadInt(ancestryStatsJson, "hp") ?? 8;
        var hpPerLevel = ReadInt(classStatsJson, "hp_per_level") ?? 8;
        var speed = ReadInt(ancestryStatsJson, "speed") ?? 25;

        var maxHp = ancestryHp + level * (hpPerLevel + conModifier);
        var proficiencyBonus = level + 2; // условно "тренированное" владение без брони
        var armorClass = 10 + dexModifier + proficiencyBonus;

        var features = ReadString(classStatsJson, "class_features");

        return new Pf2eAutomation(Math.Max(1, maxHp), armorClass, speed, features);
    }

    private static CharacterAutomationCalculator.AbilityScores ApplyBoost(
        CharacterAutomationCalculator.AbilityScores scores, string code)
    {
        int Boost(int value) => value + (value < 18 ? 2 : 1);

        return code switch
        {
            "STR" => scores with { Str = Boost(scores.Str) },
            "DEX" => scores with { Dex = Boost(scores.Dex) },
            "CON" => scores with { Con = Boost(scores.Con) },
            "INT" => scores with { Int = Boost(scores.Int) },
            "WIS" => scores with { Wis = Boost(scores.Wis) },
            "CHA" => scores with { Cha = Boost(scores.Cha) },
            _ => scores // "ANY" — свободный выбор, не применяем автоматически
        };
    }

    private static CharacterAutomationCalculator.AbilityScores ApplyFlaw(
        CharacterAutomationCalculator.AbilityScores scores, string code) => code switch
    {
        "STR" => scores with { Str = scores.Str - 2 },
        "DEX" => scores with { Dex = scores.Dex - 2 },
        "CON" => scores with { Con = scores.Con - 2 },
        "INT" => scores with { Int = scores.Int - 2 },
        "WIS" => scores with { Wis = scores.Wis - 2 },
        "CHA" => scores with { Cha = scores.Cha - 2 },
        _ => scores
    };

    private static List<string> ReadCodes(string json, string prop)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(prop, out var el) || el.ValueKind != JsonValueKind.Array)
                return [];
            return el.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
        }
        catch { return []; }
    }

    private static string? ReadFlawCode(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("flaw_code", out var el) && el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;
        }
        catch { return null; }
    }

    private static int? ReadInt(string json, string prop)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Number
                ? el.GetInt32()
                : null;
        }
        catch { return null; }
    }

    private static string? ReadString(string json, string prop)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;
        }
        catch { return null; }
    }
}
