using System.Text.Json;

namespace TTRPGHub.Services;

// L.6 — community-слой переводов поверх slug: glossary + частичные overlays для
// conditions/spells/monsters/rule entries. Нет перевода → показываем EN как есть.
public sealed class Pf2eLocaleService(HttpClient http, ContentLanguageService language)
{
    // M.4 — Heightened отдельным полем: раньше DescriptionAsync("spell", slug, ...) вызывался
    // и для базового описания, и для текста усиления с одним и тем же slug — оба возвращали
    // Description, из-за чего усиление показывало продублированный текст описания вместо
    // своего. Теперь у "усиления" собственное поле и свой метод (HeightenedAsync).
    private sealed record LocaleEntry(string? Name, string? Summary, string? Description, string? Heightened);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private Dictionary<string, string>? _glossary;
    private Dictionary<string, LocaleEntry>? _conditions;
    private Dictionary<string, LocaleEntry>? _spells;
    private Dictionary<string, LocaleEntry>? _monsters;
    private Dictionary<string, LocaleEntry>? _entries;
    private bool _loaded;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _glossary = await LoadStringMapAsync("locale/pf2e/glossary.ru.json");
        _conditions = await LoadEntryMapAsync("locale/pf2e/conditions.ru.json");
        _spells = await LoadEntryMapAsync("locale/pf2e/spells.ru.json");
        _monsters = await LoadEntryMapAsync("locale/pf2e/monsters.ru.json");
        _entries = await LoadEntryMapAsync("locale/pf2e/entries.ru.json");
        _loaded = true;
    }

    public async Task<string> NameAsync(string category, string slug, string english)
    {
        if (!language.IsRussian) return english;
        await EnsureLoadedAsync();
        var entry = Lookup(category, slug);
        return entry?.Name ?? english;
    }

    public async Task<string?> SummaryAsync(string category, string slug, string? english)
    {
        if (!language.IsRussian) return english;
        await EnsureLoadedAsync();
        var entry = Lookup(category, slug);
        return entry?.Summary ?? english;
    }

    public async Task<string> DescriptionAsync(string category, string slug, string english)
    {
        if (!language.IsRussian) return english;
        await EnsureLoadedAsync();
        var entry = Lookup(category, slug);
        return entry?.Description ?? english;
    }

    public async Task<string?> HeightenedAsync(string category, string slug, string? english)
    {
        if (!language.IsRussian) return english;
        await EnsureLoadedAsync();
        var entry = Lookup(category, slug);
        return entry?.Heightened ?? english;
    }

    public async Task<string> LocalizeCsvAsync(string? englishCsv)
    {
        if (string.IsNullOrWhiteSpace(englishCsv) || !language.IsRussian)
            return englishCsv ?? string.Empty;
        await EnsureLoadedAsync();
        if (_glossary is null || _glossary.Count == 0) return englishCsv;

        return string.Join(", ", englishCsv
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(token => LocalizeToken(token)));
    }

    // M.4 — Pf2eMonster (в отличие от Pf2eSpell) не хранит английский флейвор-текст вообще
    // (нет такого поля у нашего ORC/OGL источника монстров) — оверлей тут не "перевод
    // существующего", а единственный источник этого текста. Без английского fallback:
    // нет RU-перевода — блок с описанием просто не показывается, а не показывает пустоту.
    public async Task<string?> FlavorAsync(string category, string slug)
    {
        if (!language.IsRussian) return null;
        await EnsureLoadedAsync();
        return Lookup(category, slug)?.Description;
    }

    public bool HasTranslation(string category, string slug)
    {
        if (!language.IsRussian || !_loaded) return false;
        return Lookup(category, slug)?.Name is not null;
    }

    private string LocalizeToken(string token)
    {
        var key = token.Trim().ToLowerInvariant();
        if (_glossary!.TryGetValue(key, out var ru)) return ru;
        var slug = Pf2eLookups.SlugifyItemName(token);
        if (_glossary.TryGetValue(slug, out ru)) return ru;
        return token;
    }

    private LocaleEntry? Lookup(string category, string slug)
    {
        var map = category switch
        {
            "condition" => _conditions,
            "spell" => _spells,
            "monster" => _monsters,
            _ => _entries,
        };
        if (map is null) return null;
        return map.TryGetValue(slug, out var entry) ? entry : null;
    }

    private async Task<Dictionary<string, string>> LoadStringMapAsync(string path)
    {
        try
        {
            await using var stream = await http.GetStreamAsync(path);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task<Dictionary<string, LocaleEntry>> LoadEntryMapAsync(string path)
    {
        try
        {
            await using var stream = await http.GetStreamAsync(path);
            var raw = await JsonSerializer.DeserializeAsync<Dictionary<string, LocaleEntry>>(stream, JsonOptions)
                      ?? new Dictionary<string, LocaleEntry>(StringComparer.OrdinalIgnoreCase);
            return new Dictionary<string, LocaleEntry>(raw, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, LocaleEntry>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
