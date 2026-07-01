using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Refit;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Tickets;

public partial class MyTickets
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private string _title = string.Empty;
    private string _description = string.Empty;
    private string? _contactInfo;
    private List<IBrowserFile> _selectedFiles = [];
    private List<TicketDto> _tickets = [];
    private bool _loading = true;
    private bool _submitting;
    private string? _error;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var page = await Api.GetMyTicketsAsync();
            _tickets = page.Items;
        }
        catch { /* список останется пустым */ }
        finally { _loading = false; }
    }

    private void OnFilesSelected(InputFileChangeEventArgs e) =>
        _selectedFiles = e.GetMultipleFiles(5).ToList();

    private async Task SubmitAsync()
    {
        _submitting = true;
        _error = null;
        try
        {
            var parts = new List<StreamPart>();
            foreach (var file in _selectedFiles)
            {
                var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                parts.Add(new StreamPart(ms, file.Name, file.ContentType));
            }

            await Api.CreateTicketAsync(_title, _description, _contactInfo, parts);

            _title = string.Empty;
            _description = string.Empty;
            _contactInfo = null;
            _selectedFiles = [];
            await LoadAsync();
        }
        catch
        {
            _error = "Не удалось отправить тикет. Проверьте размер файлов (макс. 10 МБ каждый) и попробуйте снова.";
        }
        finally { _submitting = false; }
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
