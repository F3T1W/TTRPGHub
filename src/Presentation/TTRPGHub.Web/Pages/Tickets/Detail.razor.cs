using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Tickets;

public partial class Detail
{
    [Parameter] public Guid TicketId { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private TicketDto? _ticket;
    private List<TicketCommentDto> _comments = [];
    private bool _loading = true;
    private string? _error;
    private string _newComment = string.Empty;
    private bool _submitting;
    private string? _commentError;

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _ticket = await Api.GetTicketByIdAsync(TicketId);
            _comments = await Api.GetTicketCommentsAsync(TicketId);
        }
        catch
        {
            _error = "Не удалось загрузить тикет.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SubmitCommentAsync()
    {
        _submitting = true;
        _commentError = null;
        try
        {
            await Api.AddTicketCommentAsync(TicketId, new AddTicketCommentRequest(_newComment));
            _newComment = string.Empty;
            _comments = await Api.GetTicketCommentsAsync(TicketId);
        }
        catch
        {
            _commentError = "Не удалось отправить комментарий.";
        }
        finally
        {
            _submitting = false;
        }
    }

    private static string StatusLabel(string status) => status switch
    {
        "Open" => "Открыт",
        "InProgress" => "В работе",
        "Done" => "Готово",
        _ => status
    };

    private static string StatusBadgeClass(string status) => status switch
    {
        "Open" => "text-bg-secondary",
        "InProgress" => "text-bg-warning",
        "Done" => "text-bg-success",
        _ => "text-bg-secondary"
    };
}
