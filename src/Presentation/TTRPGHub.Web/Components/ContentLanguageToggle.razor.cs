using TTRPGHub.Services;

namespace TTRPGHub.Components;

public partial class ContentLanguageToggle : IDisposable
{
    protected override async Task OnInitializedAsync()
    {
        await Lang.InitializeAsync();
        Lang.OnChanged += OnLangChanged;
    }

    private async Task SetLang(Pf2eContentLanguage language)
    {
        await Lang.SetAsync(language);
    }

    private void OnLangChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Lang.OnChanged -= OnLangChanged;
}
