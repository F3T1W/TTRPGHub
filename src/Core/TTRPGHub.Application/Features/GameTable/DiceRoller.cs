using System.Text;
using System.Text.RegularExpressions;

namespace TTRPGHub.Features.GameTable;

internal static partial class DiceRoller
{
    // Поддерживает выражения вида "2d6+3", "d20", "1d100-5", максимум несколько групп через "+": "1d20+2d6+3"
    [GeneratedRegex(@"^\s*(\d*)d(\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DiceGroupRegex();

    internal static DiceRollResult? Roll(string expression, Random? random = null)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > 100)
            return null;

        random ??= Random.Shared;

        var normalized = expression.Replace(" ", "");
        var tokens = SplitTokens(normalized);
        if (tokens.Count == 0)
            return null;

        var sb = new StringBuilder();
        var total = 0;
        var firstToken = true;

        foreach (var (sign, token) in tokens)
        {
            if (int.TryParse(token, out var flat))
            {
                total += sign * flat;
                AppendTerm(sb, firstToken, sign, flat.ToString());
                firstToken = false;
                continue;
            }

            var match = DiceGroupRegex().Match(token);
            if (!match.Success)
                return null;

            var count = match.Groups[1].Value.Length == 0 ? 1 : int.Parse(match.Groups[1].Value);
            var sides = int.Parse(match.Groups[2].Value);

            if (count is < 1 or > 100 || sides is < 2 or > 1000)
                return null;

            var rolls = new int[count];
            var groupSum = 0;
            for (var i = 0; i < count; i++)
            {
                rolls[i] = random.Next(1, sides + 1);
                groupSum += rolls[i];
            }

            total += sign * groupSum;
            AppendTerm(sb, firstToken, sign, $"{count}d{sides}({string.Join(",", rolls)})");
            firstToken = false;
        }

        return new DiceRollResult(expression, total, sb.ToString());
    }

    private static void AppendTerm(StringBuilder sb, bool first, int sign, string text)
    {
        if (!first) sb.Append(sign > 0 ? " + " : " - ");
        else if (sign < 0) sb.Append('-');
        sb.Append(text);
    }

    private static List<(int Sign, string Token)> SplitTokens(string normalized)
    {
        var result = new List<(int, string)>();
        var sign = 1;
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                result.Add((sign, current.ToString()));
                current.Clear();
            }
        }

        foreach (var ch in normalized)
        {
            if (ch == '+') { Flush(); sign = 1; }
            else if (ch == '-') { Flush(); sign = -1; }
            else current.Append(ch);
        }
        Flush();

        return result;
    }
}

internal sealed record DiceRollResult(string Expression, int Total, string Breakdown);
