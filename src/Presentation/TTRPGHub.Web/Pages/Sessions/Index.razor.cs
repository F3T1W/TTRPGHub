using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Sessions;

public partial class Index
{
    private List<SessionSummaryDto> _sessions = [];
    private List<CampaignSummaryDto> _campaigns = [];
    private bool _loading = true;
    private string? _error;
    private string? _importError;
    private string? _importSuccess;
    private string _kind = "oneshot";   // oneshot | campaign
    private string _tab  = "upcoming"; // upcoming | my

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task SwitchKind(string kind)
    {
        _kind = kind;
        await LoadAsync();
    }

    private async Task SwitchTab(string tab)
    {
        _tab = tab;
        await LoadAsync();
    }

    private void GoCreate() =>
        Nav.NavigateTo(_kind == "oneshot" ? "/sessions/create" : "/campaigns/create");

    private async Task LoadAsync()
    {
        _loading = true; _error = null;
        try
        {
            if (_kind == "oneshot")
            {
                _sessions = _tab == "upcoming"
                    ? await Api.GetUpcomingSessionsAsync()
                    : await Api.GetMySessionsAsync();
            }
            else
            {
                _campaigns = _tab == "upcoming"
                    ? await Api.GetAllCampaignsAsync()
                    : await Api.GetMyCampaignsAsync();
            }
        }
        catch { _error = "Не удалось загрузить данные."; }
        finally { _loading = false; }
    }

    private static string StatusBadgeClass(SessionStatus s) => s switch
    {
        SessionStatus.Planned    => "bg-primary",
        SessionStatus.InProgress => "bg-success",
        SessionStatus.Completed  => "bg-secondary",
        SessionStatus.Cancelled  => "bg-danger",
        _                        => "bg-secondary"
    };

    private static string StatusLabel(SessionStatus s) => s switch
    {
        SessionStatus.Planned    => "Запланирована",
        SessionStatus.InProgress => "Идёт игра",
        SessionStatus.Completed  => "Завершена",
        SessionStatus.Cancelled  => "Отменена",
        _                        => s.ToString()
    };

    private static string CampaignStatusBadge(CampaignStatus s) => s switch
    {
        CampaignStatus.Active    => "bg-success",
        CampaignStatus.Paused    => "bg-warning text-dark",
        CampaignStatus.Completed => "bg-secondary",
        CampaignStatus.Archived  => "bg-dark border border-secondary",
        _                        => "bg-secondary"
    };

    private static string CampaignStatusLabel(CampaignStatus s) => s switch
    {
        CampaignStatus.Active    => "Активна",
        CampaignStatus.Paused    => "Пауза",
        CampaignStatus.Completed => "Завершена",
        CampaignStatus.Archived  => "Архив",
        _                        => s.ToString()
    };

    internal async Task ImportAsync(InputFileChangeEventArgs e)
    {
        _importError = null;
        _importSuccess = null;
        var file = e.File;
        if (!file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            _importError = "Выберите файл в формате .json";
            return;
        }
        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 512_000);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (_kind == "oneshot")
            {
                var req = await JsonSerializer.DeserializeAsync<ImportSessionRequest>(stream, opts);
                if (req is null || string.IsNullOrWhiteSpace(req.Title))
                {
                    _importError = "Файл не содержит корректных данных ваншота.";
                    return;
                }
                var result = await Api.ImportSessionAsync(req);
                _importSuccess = $"Ваншот «{result.Title}» успешно импортирован!";
            }
            else
            {
                var req = await JsonSerializer.DeserializeAsync<ImportCampaignRequest>(stream, opts);
                if (req is null || string.IsNullOrWhiteSpace(req.Title))
                {
                    _importError = "Файл не содержит корректных данных кампании.";
                    return;
                }
                var result = await Api.ImportCampaignAsync(req);
                _importSuccess = $"Кампания «{result.Title}» успешно импортирована!";
            }

            await LoadAsync();
        }
        catch (JsonException)
        {
            _importError = "Ошибка разбора JSON. Проверьте формат файла.";
        }
        catch
        {
            _importError = "Не удалось импортировать.";
        }
    }
}
