using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TTRPGHub.Features.Rules.Common;

// Транслитерация нужна, потому что заголовки кастомных систем/записей обычно на русском,
// а slug используется в URL и должен остаться на латинице.
internal static partial class SlugGenerator
{
    private static readonly Dictionary<char, string> Translit = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e",
        ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m",
        ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch",
        ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya",
    };

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"^-+|-+$")]
    private static partial Regex TrimDashesRegex();

    public static string FromTitle(string title)
    {
        var sb = new StringBuilder();
        foreach (var ch in title.ToLowerInvariant())
            sb.Append(Translit.TryGetValue(ch, out var repl) ? repl : ch.ToString());

        var normalized = sb.ToString().Normalize(NormalizationForm.FormC);
        var slug = NonAlphanumericRegex().Replace(normalized, "-");
        slug = TrimDashesRegex().Replace(slug, "");

        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
