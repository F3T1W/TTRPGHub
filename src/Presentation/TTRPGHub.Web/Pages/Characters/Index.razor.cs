using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Characters;

public partial class Index
{
    private List<CharacterSummaryDto> _characters = [];
    private string? _error;
    private string? _importError;
    private string? _importSuccess;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            await LoadAsync();
        else
            _loading = false;
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error   = null;
        try
        {
            _characters = await Api.GetMyCharactersAsync();
        }
        catch
        {
            _error = "Не удалось загрузить персонажей.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ImportCharacterAsync(InputFileChangeEventArgs e)
    {
        _importError = null;
        _importSuccess = null;
        var file = e.File;
        if (!file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            _importError = "Выберите файл в формате .json";
            return;
        }
        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 1_048_576); // 1MB
            var request = await JsonSerializer.DeserializeAsync<ImportCharacterRequest>(
                stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request is null || string.IsNullOrWhiteSpace(request.Name))
            {
                _importError = "Файл не содержит корректных данных персонажа.";
                return;
            }

            var result = await Api.ImportCharacterAsync(request);
            _importSuccess = $"Персонаж «{result.Name}» успешно импортирован!";
            await LoadAsync();
        }
        catch (JsonException)
        {
            _importError = "Ошибка разбора JSON. Проверьте формат файла.";
        }
        catch
        {
            _importError = "Не удалось импортировать персонажа.";
        }
    }
}
