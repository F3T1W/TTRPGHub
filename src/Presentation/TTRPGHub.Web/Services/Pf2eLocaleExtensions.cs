namespace TTRPGHub.Services;

// L.6 — локализованные строки для отображения в UI (кэш на странице).
public sealed record Pf2eLocalizedSpellRow(
    string Name, string Traditions, string Traits, string Cast, string? Range, string Duration);

public sealed record Pf2eLocalizedMonsterRow(string Name, string Traits, string Size);

public sealed record Pf2eLocalizedSpellDetail(
    string Name, string Traditions, string Traits, string Cast, string? Range,
    string? Area, string? Targets, string Duration, string Description, string? Heightened);

public static class Pf2eLocaleExtensions
{
    public static async Task<Pf2eLocalizedSpellRow> LocalizeAsync(this Pf2eLocaleService locale, Pf2eSpellSummaryDto spell) =>
        new(
            await locale.NameAsync("spell", spell.Slug, spell.Name),
            await locale.LocalizeCsvAsync(spell.Traditions),
            await locale.LocalizeCsvAsync(spell.Traits),
            await locale.LocalizeCsvAsync(spell.Cast),
            spell.Range,
            await locale.LocalizeCsvAsync(spell.Duration));

    public static async Task<Pf2eLocalizedMonsterRow> LocalizeAsync(this Pf2eLocaleService locale, Pf2eMonsterSummaryDto monster) =>
        new(
            await locale.NameAsync("monster", monster.Slug, monster.Name),
            await locale.LocalizeCsvAsync(monster.Traits),
            await locale.LocalizeCsvAsync(monster.Size));

    public static async Task<Pf2eLocalizedSpellDetail> LocalizeAsync(this Pf2eLocaleService locale, Pf2eSpellDetailDto spell) =>
        new(
            await locale.NameAsync("spell", spell.Slug, spell.Name),
            await locale.LocalizeCsvAsync(spell.Traditions),
            await locale.LocalizeCsvAsync(spell.Traits),
            await locale.LocalizeCsvAsync(spell.Cast),
            spell.Range, spell.Area, spell.Targets,
            await locale.LocalizeCsvAsync(spell.Duration),
            await locale.DescriptionAsync("spell", spell.Slug, spell.Description),
            spell.Heightened is null ? null : await locale.HeightenedAsync("spell", spell.Slug, spell.Heightened));
}
