using System.Text.Json;
using System.Text.RegularExpressions;

namespace TTRPGHub.Services;

public static partial class Pf2eSpellAutomation
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public sealed record DamageInstance(string Formula, string? Type, IReadOnlyList<string> Kinds, bool ApplyMod);

    public sealed record SpellDamage(IReadOnlyList<DamageInstance> Instances);

    public sealed record SpellHeightening(int Interval, IReadOnlyList<string> DamageIncrements);

    public sealed record SpellDefense(string Save, bool Basic);

    public sealed record ResolvedDamage(string Expression, bool IsHealing, bool IsDamage, string? DamageType);

    public static SpellDamage? ParseDamage(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("instances", out var instances) ||
                instances.ValueKind != JsonValueKind.Array)
                return null;

            var list = new List<DamageInstance>();
            foreach (var item in instances.EnumerateArray())
            {
                var formula = item.TryGetProperty("formula", out var f) ? f.GetString() : null;
                if (string.IsNullOrWhiteSpace(formula)) continue;
                var type = item.TryGetProperty("type", out var t) ? t.GetString() : null;
                var kinds = item.TryGetProperty("kinds", out var k) && k.ValueKind == JsonValueKind.Array
                    ? k.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
                    : [];
                var applyMod = item.TryGetProperty("applyMod", out var am) && am.GetBoolean();
                list.Add(new DamageInstance(formula, type, kinds, applyMod));
            }

            return list.Count > 0 ? new SpellDamage(list) : null;
        }
        catch
        {
            return null;
        }
    }

    public static SpellHeightening? ParseHeightening(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "interval")
                return null;
            var interval = root.TryGetProperty("interval", out var i) ? i.GetInt32() : 1;
            if (interval <= 0) interval = 1;
            if (!root.TryGetProperty("damage", out var damage) || damage.ValueKind != JsonValueKind.Array)
                return null;
            var increments = damage.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList();
            return increments.Count > 0 ? new SpellHeightening(interval, increments) : null;
        }
        catch
        {
            return null;
        }
    }

    public static SpellDefense? ParseDefense(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("save", out var saveEl)) return null;
            var save = saveEl.GetString();
            if (string.IsNullOrWhiteSpace(save)) return null;
            var basic = root.TryGetProperty("basic", out var b) && b.GetBoolean();
            return new SpellDefense(save, basic);
        }
        catch
        {
            return null;
        }
    }

    public static int HeightenSteps(int baseLevel, int castLevel, SpellHeightening? heightening)
    {
        if (heightening is null || castLevel <= baseLevel) return 0;
        return (castLevel - baseLevel) / heightening.Interval;
    }

    public static IReadOnlyList<ResolvedDamage> ResolveDamage(
        SpellDamage damage,
        SpellHeightening? heightening,
        int baseLevel,
        int castLevel,
        int abilityMod)
    {
        var steps = HeightenSteps(baseLevel, castLevel, heightening);
        var resolved = new List<ResolvedDamage>();

        for (var i = 0; i < damage.Instances.Count; i++)
        {
            var instance = damage.Instances[i];
            var formula = instance.Formula;
            if (steps > 0 && heightening is not null && i < heightening.DamageIncrements.Count)
            {
                var increment = heightening.DamageIncrements[i];
                for (var step = 0; step < steps; step++)
                    formula = CombineFormulas(formula, increment);
            }

            if (instance.ApplyMod && abilityMod != 0)
                formula = ApplyFlatModifier(formula, abilityMod);

            var isHealing = instance.Kinds.Any(k => k.Equals("healing", StringComparison.OrdinalIgnoreCase))
                && !instance.Kinds.Any(k => k.Equals("damage", StringComparison.OrdinalIgnoreCase));
            var isDamage = instance.Kinds.Any(k => k.Equals("damage", StringComparison.OrdinalIgnoreCase))
                && !isHealing;
            if (!isHealing && !isDamage)
                isDamage = true;

            resolved.Add(new ResolvedDamage(formula, isHealing, isDamage, instance.Type));
        }

        return resolved;
    }

    private static string CombineFormulas(string left, string right)
    {
        var l = ParseFormula(left);
        var r = ParseFormula(right);
        var diceByFaces = new Dictionary<int, int>();
        foreach (var (count, faces) in l.Dice.Concat(r.Dice))
            diceByFaces[faces] = diceByFaces.GetValueOrDefault(faces) + count;
        return BuildFormula(diceByFaces, l.Flat + r.Flat);
    }

    private static string ApplyFlatModifier(string formula, int mod)
    {
        var parsed = ParseFormula(formula);
        var diceByFaces = parsed.Dice
            .GroupBy(d => d.Faces)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        return BuildFormula(diceByFaces, parsed.Flat + mod);
    }

    private sealed record ParsedFormula(List<(int Count, int Faces)> Dice, int Flat);

    private static ParsedFormula ParseFormula(string formula)
    {
        var normalized = formula.Replace(" ", "");
        var dice = new List<(int Count, int Faces)>();
        foreach (Match match in DiceTokenRegex().Matches(normalized))
        {
            var count = int.Parse(match.Groups[1].Value);
            var faces = int.Parse(match.Groups[2].Value);
            dice.Add((count, faces));
        }

        var withoutDice = DiceTokenRegex().Replace(normalized, "");
        var flat = 0;
        foreach (Match match in FlatModifierRegex().Matches(withoutDice))
            flat += int.Parse(match.Value);

        return new ParsedFormula(dice, flat);
    }

    private static string BuildFormula(Dictionary<int, int> diceByFaces, int flat)
    {
        var parts = diceByFaces
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Key)
            .Select(kv => $"{kv.Value}d{kv.Key}")
            .ToList();

        // No leading "+" here: string.Join("+", parts) below already inserts the separator between
        // parts, so prefixing this one too used to double up into e.g. "1d8++4" whenever dice were
        // also present.
        if (flat != 0)
            parts.Add(flat.ToString());

        return parts.Count > 0 ? string.Join("+", parts).Replace("+-", "-") : "0";
    }

    [GeneratedRegex(@"(?:(\d+)d(\d+))", RegexOptions.Compiled)]
    private static partial Regex DiceTokenRegex();

    [GeneratedRegex(@"[+-]\d+", RegexOptions.Compiled)]
    private static partial Regex FlatModifierRegex();
}
