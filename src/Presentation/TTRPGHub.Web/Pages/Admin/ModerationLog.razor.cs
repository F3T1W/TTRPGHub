using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Admin;

public partial class ModerationLog
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private List<ModerationLogEntryDto> _entries = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try { _entries = await Api.GetModerationLogAsync(); }
        catch { _error = "Не удалось загрузить журнал модерации."; }
        finally { _loading = false; }
    }

    private static string ActionLabel(string action) => action switch
    {
        "DeleteTopic" => "Удаление темы",
        "DeletePost" => "Удаление сообщения",
        "PinTopic" => "Закрепление темы",
        "UnpinTopic" => "Открепление темы",
        "LockTopic" => "Блокировка темы",
        "UnlockTopic" => "Разблокировка темы",
        _ when action.StartsWith("ResolveReport:") => $"Разбор жалобы ({action["ResolveReport:".Length..]})",
        _ => action
    };
}
