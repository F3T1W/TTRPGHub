using System.Text.Json;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Rules;

public partial class Detail
{
    [Parameter] public string SystemSlug { get; set; } = "";
    [Parameter] public string Category { get; set; } = "";
    [Parameter] public string Slug { get; set; } = "";
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private static readonly MarkdownPipeline MarkdownPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private RuleEntryDetailDto? _entry;
    private readonly List<(string Label, string Value)> _statFields = [];
    private readonly List<(string Label, MarkupString Html)> _blockFields = [];
    private MarkupString? _contentHtml;
    private bool _loading = true;
    private bool _deleting;
    private string? _error;
    private string? _deleteError;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _error = null;
        _statFields.Clear();
        _blockFields.Clear();
        _contentHtml = null;

        try
        {
            _entry = await Api.GetRuleEntryDetailAsync(SystemSlug, Category, Slug);
            ParseStats(_entry.StatsJson);
            if (!string.IsNullOrWhiteSpace(_entry.ContentMarkdown))
                _contentHtml = ToHtml(_entry.ContentMarkdown);
        }
        catch
        {
            _entry = null;
            _error = "Запись не найдена.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (_entry is null) return;
        _deleting = true;
        _deleteError = null;
        try
        {
            await Api.DeleteRuleEntryAsync(SystemSlug, Category, Slug);
            Nav.NavigateTo($"/reference/{SystemSlug}/rules/{Category}");
        }
        catch
        {
            _deleteError = "Не удалось удалить запись.";
            _deleting = false;
        }
    }

    private void ParseStats(string statsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(statsJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.String)
                {
                    var scalar = FormatScalar(prop.Value);
                    if (scalar is not null) _statFields.Add((PrettyLabel(prop.Name), scalar));
                    continue;
                }

                var text = prop.Value.GetString();
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Многострочные значения (таблицы прогрессии, длинные описания) рендерим как markdown-блок,
                // короткие однострочные — как обычную пару "метка: значение"
                if (text.Contains('\n') || text.Contains('|') || text.Length > 200)
                    _blockFields.Add((PrettyLabel(prop.Name), ToHtml(text)));
                else
                    _statFields.Add((PrettyLabel(prop.Name), text));
            }
        }
        catch { /* некорректный JSON — просто не показываем характеристики */ }
    }

    private static MarkupString ToHtml(string markdown) =>
        new(Markdown.ToHtml(markdown, MarkdownPipeline));

    private static string? FormatScalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.ToString(),
        JsonValueKind.True => "да",
        JsonValueKind.False => "нет",
        JsonValueKind.Array when value.GetArrayLength() > 0 && value[0].ValueKind == JsonValueKind.String =>
            string.Join(", ", value.EnumerateArray().Select(e => e.GetString())),
        _ => null
    };

    private static string PrettyLabel(string key) =>
        char.ToUpperInvariant(key[0]) + key[1..].Replace('_', ' ');

    private static string CategoryLabel(string category) => category.ToLowerInvariant() switch
    {
        "spell" => "Заклинания",
        "monster" => "Монстры",
        "class" => "Классы",
        "race" => "Расы",
        "feat" => "Фиты",
        "action" => "Действия",
        "condition" => "Состояния",
        "equipment" => "Снаряжение",
        "background" => "Предыстории",
        "rule" => "Правила",
        _ => category
    };
}
