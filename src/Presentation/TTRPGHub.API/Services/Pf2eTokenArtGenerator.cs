namespace TTRPGHub.Services;

// K.3 — уникальный токен-арт монстров: детерминированный SVG из данных статблока, без внешних
// лицензий и хранилища. База — глиф типа существа (те же формы, что статичные плейсхолдеры J.6
// в Web/wwwroot/img/tokens), уникальность — цветовая гамма и кольцо декоративных меток, стабильно
// выводимые из хеша слага (FNV-1a, а не string.GetHashCode — тот рандомизирован между запусками).
// Цветовые слова в имени (White Dragon, Red Dragon...) дают настоящий цвет, а не хешевый.
internal static class Pf2eTokenArtGenerator
{
    private static readonly (string Word, int Hue, int Sat, int Light)[] ColorWords =
    [
        ("white", 220, 15, 85), ("black", 260, 20, 25), ("red", 0, 70, 55),
        ("green", 130, 55, 45), ("blue", 215, 70, 55), ("brass", 40, 60, 55),
        ("bronze", 30, 55, 45), ("copper", 20, 65, 50), ("gold", 45, 80, 55),
        ("silver", 210, 12, 70), ("purple", 275, 60, 60), ("crimson", 348, 70, 45),
    ];

    private static readonly (string Trait, string Glyph)[] Glyphs =
    [
        ("dragon", """<path fill="{A}" d="M20 65 L35 40 L45 50 L55 30 L65 45 L75 35 L70 60 L60 55 L50 68 L40 58 Z"/><circle cx="65" cy="38" r="3" fill="{G}"/>"""),
        ("undead", """<circle cx="50" cy="42" r="22" fill="none" stroke="{A}" stroke-width="4"/><circle cx="41" cy="38" r="5" fill="{A}"/><circle cx="59" cy="38" r="5" fill="{A}"/><path d="M37 52 Q50 62 63 52" stroke="{A}" stroke-width="4" fill="none"/><path d="M50 64 L50 78" stroke="{A}" stroke-width="4"/>"""),
        ("humanoid", """<circle cx="50" cy="35" r="14" fill="{A}"/><path d="M28 78 Q50 55 72 78" stroke="{A}" stroke-width="8" fill="none" stroke-linecap="round"/>"""),
        ("animal", """<path fill="{A}" d="M50 75 Q30 75 30 55 Q30 45 40 45 Q40 30 50 30 Q60 30 60 45 Q70 45 70 55 Q70 75 50 75 Z"/><circle cx="30" cy="30" r="6" fill="{A}"/><circle cx="70" cy="30" r="6" fill="{A}"/>"""),
        ("beast", """<path fill="{A}" d="M50 75 Q30 75 30 55 Q30 45 40 45 Q40 30 50 30 Q60 30 60 45 Q70 45 70 55 Q70 75 50 75 Z"/><circle cx="30" cy="30" r="6" fill="{A}"/><circle cx="70" cy="30" r="6" fill="{A}"/>"""),
        ("aberration", """<circle cx="50" cy="45" r="20" fill="none" stroke="{A}" stroke-width="4"/><circle cx="50" cy="45" r="8" fill="{A}"/><path d="M30 65 Q20 78 15 88 M50 68 Q50 82 50 92 M70 65 Q80 78 85 88" stroke="{A}" stroke-width="4" fill="none" stroke-linecap="round"/>"""),
        ("construct", """<rect x="32" y="28" width="36" height="32" rx="4" fill="none" stroke="{A}" stroke-width="4"/><circle cx="42" cy="42" r="3" fill="{G}"/><circle cx="58" cy="42" r="3" fill="{G}"/><path d="M40 60 L40 78 M60 60 L60 78" stroke="{A}" stroke-width="4"/><path d="M50 20 L50 28" stroke="{A}" stroke-width="4"/><circle cx="50" cy="17" r="3" fill="{A}"/>"""),
        ("elemental", """<path fill="{G}" d="M50 20 Q35 45 40 58 Q42 68 50 70 Q58 68 60 58 Q65 45 50 20 Z"/><path fill="{A}" d="M50 40 Q44 52 47 60 Q48 65 50 66 Q52 65 53 60 Q56 52 50 40 Z"/>"""),
        ("fiend", """<path d="M35 30 L45 40 M65 30 L55 40" stroke="{A}" stroke-width="5" stroke-linecap="round" fill="none"/><circle cx="50" cy="50" r="20" fill="none" stroke="{A}" stroke-width="4"/><circle cx="43" cy="46" r="3" fill="#ef4444"/><circle cx="57" cy="46" r="3" fill="#ef4444"/><path d="M40 58 Q50 65 60 58" stroke="{A}" stroke-width="3" fill="none"/>"""),
        ("celestial", """<circle cx="50" cy="42" r="14" fill="{G}"/><path d="M20 55 Q35 35 48 45 M80 55 Q65 35 52 45" stroke="{A}" stroke-width="4" fill="none" stroke-linecap="round"/>"""),
        ("plant", """<path d="M50 80 L50 45" stroke="{A}" stroke-width="5"/><path fill="{A}" d="M50 50 Q30 40 30 20 Q50 25 50 50 Z"/><path fill="{A}" d="M50 50 Q70 40 70 20 Q50 25 50 50 Z"/>"""),
        ("ooze", """<path fill="{A}" fill-opacity="0.75" d="M30 55 Q25 35 45 32 Q50 25 60 32 Q75 32 72 52 Q75 70 55 72 Q35 75 30 55 Z"/>"""),
        ("fungus", """<path fill="{A}" d="M25 50 Q25 25 50 25 Q75 25 75 50 Z"/><rect x="45" y="50" width="10" height="25" fill="{A}"/>"""),
        ("giant", """<circle cx="50" cy="32" r="12" fill="{A}"/><rect x="38" y="44" width="24" height="30" rx="6" fill="{A}"/><rect x="25" y="48" width="10" height="22" rx="4" fill="{A}"/><rect x="65" y="48" width="10" height="22" rx="4" fill="{A}"/>"""),
    ];

    private const string GenericGlyph =
        """<text x="50" y="65" font-size="45" font-weight="700" fill="{A}" text-anchor="middle" font-family="sans-serif">?</text>""";

    internal static string GenerateSvg(string slug, string name, string traits)
    {
        var hash = Fnv1a(slug);

        var (hue, sat, light) = ColorFor(name, hash);
        var accent = $"hsl({hue},{sat}%,{light}%)";
        var ring = $"hsl({hue},{Math.Min(sat + 10, 90)}%,{Math.Max(light - 22, 12)}%)";
        var bg = $"hsl({hue},{Math.Min(sat, 40)}%,10%)";
        var gold = $"hsl({(hue + 150) % 360},70%,55%)";

        var traitList = traits.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var glyph = GenericGlyph;
        foreach (var (trait, g) in Glyphs)
        {
            if (traitList.Any(t => t.Equals(trait, StringComparison.OrdinalIgnoreCase)))
            {
                glyph = g;
                break;
            }
        }

        glyph = glyph.Replace("{A}", accent).Replace("{G}", gold);

        // Кольцо декоративных меток: количество, фаза и форма — из хеша, чтобы даже монстры
        // одного типа и близкой гаммы различались рисунком по краю.
        var tickCount = 5 + (int)(hash >> 8 & 0x7);
        var phase = (hash >> 16 & 0xFF) / 255.0 * Math.PI * 2;
        var ticksAreDots = (hash >> 24 & 1) == 0;
        var ticks = new System.Text.StringBuilder();
        for (var i = 0; i < tickCount; i++)
        {
            var angle = phase + i * Math.PI * 2 / tickCount;
            var cx = 50 + 44.5 * Math.Cos(angle);
            var cy = 50 + 44.5 * Math.Sin(angle);
            if (ticksAreDots)
                ticks.Append($"""<circle cx="{cx:F1}" cy="{cy:F1}" r="1.8" fill="{accent}"/>""");
            else
            {
                var x2 = 50 + 40 * Math.Cos(angle);
                var y2 = 50 + 40 * Math.Sin(angle);
                ticks.Append($"""<line x1="{cx:F1}" y1="{cy:F1}" x2="{x2:F1}" y2="{y2:F1}" stroke="{accent}" stroke-width="1.5"/>""");
            }
        }

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
              <circle cx="50" cy="50" r="49" fill="{bg}" stroke="{ring}" stroke-width="2.5"/>
              {ticks}
              {glyph}
            </svg>
            """;
    }

    private static (int Hue, int Sat, int Light) ColorFor(string name, uint hash)
    {
        var lower = name.ToLowerInvariant();
        foreach (var (word, hue, sat, light) in ColorWords)
            if (lower.Contains(word))
                return (hue, sat, light);

        return ((int)(hash % 360), 55 + (int)(hash >> 4 & 0xF), 62 + (int)(hash >> 12 & 0x7));
    }

    private static uint Fnv1a(string input)
    {
        var hash = 2166136261u;
        foreach (var c in input)
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return hash;
    }
}
