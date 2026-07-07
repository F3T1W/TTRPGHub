using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

public partial class SpellDetail : IDisposable
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private Pf2eLocaleService Locale { get; set; } = default!;
    [Inject] private ContentLanguageService Lang { get; set; } = default!;

    private Pf2eSpellDetailDto? _spell;
    private Pf2eLocalizedSpellDetail? _display;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await Lang.InitializeAsync();
        Lang.OnChanged += OnLanguageChanged;
        await LoadAsync();
    }

    private async void OnLanguageChanged() => await InvokeAsync(LoadAsync);

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _spell = await Api.GetPf2eSpellAsync(Id);
            _display = _spell is null ? null : await Locale.LocalizeAsync(_spell);
        }
        catch { _spell = null; _display = null; }
        finally { _loading = false; }
    }

    private static string LevelLabel(int level) =>
        level == 0 ? "Заговор" : $"Заклинание {level}-го уровня";

    private static string LevelBadge(int level) => level switch
    {
        0 => "bg-secondary",
        1 or 2 or 3 => "bg-info text-dark",
        4 or 5 or 6 => "bg-primary",
        7 or 8 => "bg-warning text-dark",
        9 => "bg-danger",
        _ => "bg-dark border border-warning text-warning"
    };

    public void Dispose() => Lang.OnChanged -= OnLanguageChanged;
}
