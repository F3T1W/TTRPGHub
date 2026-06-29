using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Events;

public partial class Index
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private EventsPagedResult? _result;
    private bool _loading = true;
    private int _page = 1;
    private const int PageSize = 12;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try { _result = await Api.GetEventsAsync(_page, PageSize); }
        finally { _loading = false; }
    }

    private async Task PrevPage() { _page--; await LoadAsync(); }
    private async Task NextPage() { _page++; await LoadAsync(); }

    private static string FormatLabel(string f) => f switch
    {
        "Online" => "Онлайн",
        "Offline" => "Оффлайн",
        _ => "Гибрид"
    };

    private static string FormatColor(string f) => f switch
    {
        "Online" => "var(--ta-accent)",
        "Offline" => "#0d6efd",
        _ => "#198754"
    };
}
