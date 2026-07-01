using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Admin;

public partial class TicketsBoard
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private static readonly string[] _columns = ["Open", "InProgress", "Done"];

    private List<TicketDto> _tickets = [];
    private bool _loading = true;
    private Guid? _draggedId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _tickets = await Api.GetAllTicketsAsync();
        }
        catch { /* доска останется пустой */ }
        finally { _loading = false; }
    }

    private List<TicketDto> TicketsIn(string status) =>
        _tickets.Where(t => t.Status == status).ToList();

    private static void AllowDrop() { }

    private async Task DropAsync(string newStatus)
    {
        if (_draggedId is not { } id) return;

        var idx = _tickets.FindIndex(t => t.Id == id);
        if (idx < 0 || _tickets[idx].Status == newStatus) return;

        var previous = _tickets[idx];
        _tickets[idx] = previous with { Status = newStatus };
        StateHasChanged();
        _draggedId = null;

        try
        {
            await Api.ChangeTicketStatusAsync(id, new ChangeTicketStatusRequest(newStatus));
        }
        catch
        {
            _tickets[idx] = previous;
            StateHasChanged();
        }
    }

    private static string ColumnLabel(string status) => status switch
    {
        "Open" => "Открыт",
        "InProgress" => "В работе",
        "Done" => "Готово",
        _ => status
    };
}
