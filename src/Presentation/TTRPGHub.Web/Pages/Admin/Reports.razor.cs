using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Admin;

public partial class Reports
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private const string ResolvedStatus = "Resolved";
    private const string DismissedStatus = "Dismissed";

    private List<ContentReportDto> _reports = [];
    private bool _loading = true;
    private string? _error;
    private Guid? _resolvingId;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try { _reports = await Api.GetOpenReportsAsync(); }
        catch { _error = "Не удалось загрузить жалобы."; }
        finally { _loading = false; }
    }

    private async Task ResolveAsync(Guid reportId, string status)
    {
        _resolvingId = reportId;
        _error = null;
        try
        {
            await Api.ResolveReportAsync(reportId, new ResolveReportRequest(status));
            _reports.RemoveAll(r => r.Id == reportId);
        }
        catch
        {
            _error = "Не удалось обработать жалобу.";
        }
        finally
        {
            _resolvingId = null;
        }
    }

    private static string EntityTypeLabel(string type) => type switch
    {
        "ForumPost" => "Пост на форуме",
        "ForumTopic" => "Тема на форуме",
        "HomebrewItem" => "Homebrew",
        "Rating" => "Отзыв",
        "DiscussionPost" => "Обсуждение",
        _ => type
    };
}
