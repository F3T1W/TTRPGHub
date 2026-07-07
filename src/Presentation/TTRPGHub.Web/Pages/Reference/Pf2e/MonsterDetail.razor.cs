using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

public partial class MonsterDetail : IDisposable
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private Pf2eLocaleService Locale { get; set; } = default!;
    [Inject] private ContentLanguageService Lang { get; set; } = default!;

    private Pf2eMonsterDetailDto? _monster;
    private string? _displayName;
    private string? _displaySize;
    private string? _displayTraits;
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
            _monster = await Api.GetPf2eMonsterAsync(Id);
            if (_monster is null)
            {
                _displayName = _displaySize = _displayTraits = null;
                return;
            }
            _displayName = await Locale.NameAsync("monster", _monster.Slug, _monster.Name);
            _displaySize = await Locale.LocalizeCsvAsync(_monster.Size);
            _displayTraits = await Locale.LocalizeCsvAsync(_monster.Traits);
        }
        catch { _monster = null; }
        finally { _loading = false; }
    }

    private static string Modifier(int mod) => mod >= 0 ? $"+{mod}" : mod.ToString();

    private static string LevelBadge(int level) => level switch
    {
        <= 0 => "bg-success",
        <= 3 => "bg-info text-dark",
        <= 6 => "bg-warning text-dark",
        <= 10 => "bg-danger",
        _ => "bg-dark border border-danger text-danger"
    };

    public void Dispose() => Lang.OnChanged -= OnLanguageChanged;
}
