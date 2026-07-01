using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Rules;

public partial class Edit
{
    [Parameter] public string SystemSlug { get; set; } = "";
    [Parameter] public string Category { get; set; } = "";
    [Parameter] public string? Slug { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool IsEditMode => !string.IsNullOrEmpty(Slug);

    private static readonly string[] AllCategories =
        ["spell", "monster", "class", "race", "feat", "condition", "equipment", "background", "rule"];

    private string _category = "rule";
    private string _title = "";
    private string? _summary;
    private string? _contentMarkdown;
    private string _statsJson = "{}";
    private string _tagsInput = "";
    private bool _loading;
    private bool _submitting;
    private string? _error;

    protected override async Task OnParametersSetAsync()
    {
        _category = Category.ToLowerInvariant();

        if (!IsEditMode) return;

        _loading = true;
        try
        {
            var entry = await Api.GetRuleEntryDetailAsync(SystemSlug, Category, Slug!);
            _title = entry.Title;
            _summary = entry.Summary;
            _contentMarkdown = entry.ContentMarkdown;
            _statsJson = entry.StatsJson;
            _tagsInput = string.Join(", ", entry.Tags);
        }
        catch
        {
            _error = "Не удалось загрузить запись.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SubmitAsync()
    {
        _submitting = true;
        _error = null;

        var tags = _tagsInput.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
        var request = new CreateRuleEntryRequest(_title, _summary, _contentMarkdown, _statsJson, tags);

        try
        {
            if (IsEditMode)
            {
                await Api.UpdateRuleEntryAsync(SystemSlug, Category, Slug!, request);
                Nav.NavigateTo($"/reference/{SystemSlug}/rules/{Category}/{Slug}");
            }
            else
            {
                var response = await Api.CreateRuleEntryAsync(SystemSlug, _category, request);
                Nav.NavigateTo($"/reference/{SystemSlug}/rules/{_category}/{response.Slug}");
            }
        }
        catch
        {
            _error = "Не удалось сохранить. Проверьте, что JSON характеристик корректен и вы — владелец системы.";
        }
        finally
        {
            _submitting = false;
        }
    }

    private static string CategoryLabel(string category) => category switch
    {
        "spell" => "Заклинание",
        "monster" => "Монстр",
        "class" => "Класс",
        "race" => "Раса",
        "feat" => "Фит",
        "condition" => "Состояние",
        "equipment" => "Снаряжение",
        "background" => "Предыстория",
        "rule" => "Правило",
        _ => category
    };
}
