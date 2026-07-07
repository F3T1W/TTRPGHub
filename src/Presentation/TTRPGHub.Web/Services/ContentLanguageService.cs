using Microsoft.JSInterop;

namespace TTRPGHub.Services;

// L.6 — предпочтение языка отображения PF2e-контента (EN оригинал / RU overlay по slug).
public enum Pf2eContentLanguage { En, Ru }

public sealed class ContentLanguageService(IJSRuntime js)
{
    private const string StorageKey = "ta_pf2e_content_lang";
    private bool _initialized;

    public Pf2eContentLanguage Current { get; private set; } = Pf2eContentLanguage.Ru;

    public event Action? OnChanged;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        Current = stored == "en" ? Pf2eContentLanguage.En : Pf2eContentLanguage.Ru;
    }

    public async Task SetAsync(Pf2eContentLanguage language)
    {
        await InitializeAsync();
        if (Current == language) return;
        Current = language;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, language == Pf2eContentLanguage.En ? "en" : "ru");
        OnChanged?.Invoke();
    }

    public bool IsRussian => Current == Pf2eContentLanguage.Ru;
}
