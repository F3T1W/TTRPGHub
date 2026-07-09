using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Components.Characters;

public partial class CompanionSheet
{
    [Parameter, EditorRequired] public CharacterDetailDto Character { get; set; } = default!;

    [Inject] private IApiClient Api { get; set; } = default!;

    private List<CompanionDto> _companions = [];
    private bool _loading = true;
    private bool _adding;
    private Guid? _editingId;
    private bool _saving;
    private string? _error;

    private string _name = "";
    private string _kind = "Компаньон";
    private int _level = 1;
    private int _maxHp;
    private int _currentHp;
    private int? _ac;
    private string _speed = "";
    private string _attacksText = "";
    private string _abilitiesText = "";
    private string _notes = "";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try { _companions = await Api.GetCompanionsAsync(Character.Id); }
        catch { _error = "Не удалось загрузить компаньонов."; }
        finally { _loading = false; }
    }

    private void StartAdd()
    {
        _adding = true;
        _editingId = null;
        _error = null;
        _name = ""; _kind = "Компаньон"; _level = Character.Level;
        _maxHp = 0; _currentHp = 0; _ac = null;
        _speed = ""; _attacksText = ""; _abilitiesText = ""; _notes = "";
    }

    private void StartEdit(CompanionDto c)
    {
        _adding = true;
        _editingId = c.Id;
        _error = null;
        _name = c.Name; _kind = c.Kind; _level = c.Level;
        _maxHp = c.MaxHitPoints; _currentHp = c.CurrentHitPoints; _ac = c.ArmorClass;
        _speed = c.Speed ?? ""; _attacksText = c.AttacksText ?? "";
        _abilitiesText = c.AbilitiesText ?? ""; _notes = c.Notes ?? "";
    }

    private void CancelAdd() { _adding = false; _editingId = null; }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            _error = "Укажите имя компаньона.";
            return;
        }

        _saving = true;
        _error = null;
        try
        {
            if (_editingId is { } id)
            {
                var request = new UpdateCompanionRequest(
                    _name, _kind, _level, _maxHp, _currentHp, _ac,
                    string.IsNullOrWhiteSpace(_speed) ? null : _speed,
                    string.IsNullOrWhiteSpace(_attacksText) ? null : _attacksText,
                    string.IsNullOrWhiteSpace(_abilitiesText) ? null : _abilitiesText,
                    string.IsNullOrWhiteSpace(_notes) ? null : _notes);
                await Api.UpdateCompanionAsync(Character.Id, id, request);
            }
            else
            {
                var request = new CreateCompanionRequest(
                    _name, _kind, _level, _maxHp, _ac,
                    string.IsNullOrWhiteSpace(_speed) ? null : _speed,
                    string.IsNullOrWhiteSpace(_attacksText) ? null : _attacksText,
                    string.IsNullOrWhiteSpace(_abilitiesText) ? null : _abilitiesText,
                    string.IsNullOrWhiteSpace(_notes) ? null : _notes);
                await Api.CreateCompanionAsync(Character.Id, request);
            }

            await LoadAsync();
            CancelAdd();
        }
        catch { _error = "Не удалось сохранить компаньона."; }
        finally { _saving = false; }
    }

    private async Task DeleteAsync(Guid companionId)
    {
        try
        {
            await Api.DeleteCompanionAsync(Character.Id, companionId);
            await LoadAsync();
        }
        catch { _error = "Не удалось удалить компаньона."; }
    }

    private async Task AdjustHpAsync(CompanionDto c, int delta)
    {
        var newHp = Math.Clamp(c.CurrentHitPoints + delta, 0, c.MaxHitPoints);
        if (newHp == c.CurrentHitPoints) return;

        var idx = _companions.FindIndex(x => x.Id == c.Id);
        if (idx >= 0) _companions[idx] = c with { CurrentHitPoints = newHp };

        try
        {
            var request = new UpdateCompanionRequest(
                c.Name, c.Kind, c.Level, c.MaxHitPoints, newHp, c.ArmorClass,
                c.Speed, c.AttacksText, c.AbilitiesText, c.Notes);
            await Api.UpdateCompanionAsync(Character.Id, c.Id, request);
        }
        catch { _error = "Не удалось обновить ХП компаньона."; }
    }
}
