using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Tools;

public partial class Multiclass
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private const string SystemSlug = "dnd5e";

    private List<RuleEntrySummaryDto> _classes = [];
    private readonly List<RowModel> _rows = [new()];
    private MulticlassResultDto? _result;
    private bool _loading;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var classes = await Api.GetRuleEntriesAsync(SystemSlug, "class", pageSize: 100);
            _classes = classes.Items;
        }
        catch { /* список останется пустым, поле выбора класса просто не заполнится */ }
    }

    private void AddRow() => _rows.Add(new RowModel());

    private void RemoveRow(int index)
    {
        if (_rows.Count > 1) _rows.RemoveAt(index);
    }

    private async Task CalculateAsync()
    {
        _loading = true;
        _error = null;
        _result = null;

        var classes = _rows
            .Where(r => !string.IsNullOrEmpty(r.ClassSlug))
            .Select(r => new ClassLevelInputDto(r.ClassSlug, r.Level))
            .ToList();

        if (classes.Count == 0)
        {
            _error = "Выбери хотя бы один класс.";
            _loading = false;
            return;
        }

        try
        {
            _result = await Api.CalculateMulticlassAsync(SystemSlug, classes);
        }
        catch
        {
            _error = "Не удалось посчитать. Проверь, что все классы выбраны, а суммарный уровень не выше 20.";
        }
        finally
        {
            _loading = false;
        }
    }

    private sealed class RowModel
    {
        public string ClassSlug { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
    }
}
