using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Refit;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Modules;

public partial class Index
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private List<MacroDto> _myMacros = [];
    private List<GameSystemDto> _mySystems = [];
    private HashSet<Guid> _selectedMacroIds = [];

    private string _exportName = string.Empty;
    private string _exportDescription = string.Empty;
    private string _exportVersion = "1.0.0";
    private string _exportSystemSlug = string.Empty;
    private bool _exporting;
    private string? _exportError;

    private bool _importing;
    private string? _importError;
    private ImportModuleResponse? _importResult;

    protected override async Task OnInitializedAsync()
    {
        try { _myMacros = await Api.GetMyMacrosAsync(); } catch { /* список останется пустым */ }
        try
        {
            var systems = await Api.GetGameSystemsAsync();
            _mySystems = systems.Where(s => s.IsMine).ToList();
        }
        catch { /* список останется пустым */ }
    }

    private void ToggleMacro(Guid id, bool selected)
    {
        if (selected) _selectedMacroIds.Add(id);
        else _selectedMacroIds.Remove(id);
    }

    private async Task ExportAsync()
    {
        _exporting = true;
        _exportError = null;
        try
        {
            var json = await Api.ExportModuleAsync(new ExportModuleRequest(
                _exportName, string.IsNullOrWhiteSpace(_exportDescription) ? null : _exportDescription,
                _exportVersion, _selectedMacroIds.ToList(),
                string.IsNullOrWhiteSpace(_exportSystemSlug) ? null : _exportSystemSlug));

            var filename = $"{_exportName.Replace(" ", "_")}.module.json";
            await Js.InvokeVoidAsync("downloadJson", filename, json);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            _exportError = "Модуль пуст — выбери хотя бы один макрос или свою систему справочника.";
        }
        catch
        {
            _exportError = "Не удалось собрать модуль.";
        }
        finally
        {
            _exporting = false;
        }
    }

    private async Task ImportAsync(InputFileChangeEventArgs e)
    {
        _importing = true;
        _importError = null;
        _importResult = null;
        try
        {
            var stream = e.File.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var part = new StreamPart(ms, e.File.Name, "application/json");

            _importResult = await Api.ImportModuleAsync(part);
            _myMacros = await Api.GetMyMacrosAsync();
            var systems = await Api.GetGameSystemsAsync();
            _mySystems = systems.Where(s => s.IsMine).ToList();
        }
        catch
        {
            _importError = "Не удалось импортировать модуль. Проверь формат файла (макс. 5 МБ).";
        }
        finally
        {
            _importing = false;
        }
    }
}
