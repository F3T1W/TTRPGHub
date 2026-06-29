using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Homebrew;

public partial class Create
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string _title = "";
    private string _description = "";
    private string _system = "";
    private HomebrewType _type = HomebrewType.Other;
    private string _content = "";
    private string _tags = "";
    private string? _error;
    private bool _saving;

    private async Task SubmitAsync()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_title) || string.IsNullOrWhiteSpace(_description)
            || string.IsNullOrWhiteSpace(_system) || string.IsNullOrWhiteSpace(_content))
        {
            _error = "Заполни все обязательные поля";
            return;
        }

        _saving = true;
        try
        {
            var id = await Api.CreateHomebrewAsync(new CreateHomebrewRequest(_title, _description, _system, _type, _content, _tags));
            Nav.NavigateTo($"/homebrew/{id}");
        }
        catch
        {
            _error = "Не удалось создать материал";
            _saving = false;
        }
    }

    private static string TypeLabel(HomebrewType t) => t switch
    {
        HomebrewType.Spell => "Заклинание",
        HomebrewType.Monster => "Существо",
        HomebrewType.Class => "Класс",
        HomebrewType.Subclass => "Подкласс",
        HomebrewType.Race => "Раса",
        HomebrewType.Subrace => "Подраса",
        HomebrewType.Item => "Предмет",
        HomebrewType.Background => "Предыстория",
        HomebrewType.Feat => "Черта",
        HomebrewType.Other => "Другое",
        _ => t.ToString()
    };
}
