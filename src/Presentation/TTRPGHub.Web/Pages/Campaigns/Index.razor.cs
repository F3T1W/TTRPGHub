using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Campaigns;

public partial class Index
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private List<CampaignSummaryDto> _campaigns = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try { _campaigns = await Api.GetMyCampaignsAsync(); }
        finally { _loading = false; }
    }

    private static string StatusBadge(CampaignStatus status) => status switch
    {
        CampaignStatus.Active    => "bg-success",
        CampaignStatus.Paused    => "bg-warning text-dark",
        CampaignStatus.Completed => "bg-secondary",
        CampaignStatus.Archived  => "bg-dark border border-secondary",
        _                        => "bg-secondary"
    };

    private static string StatusLabel(CampaignStatus status) => status switch
    {
        CampaignStatus.Active    => "Активна",
        CampaignStatus.Paused    => "Пауза",
        CampaignStatus.Completed => "Завершена",
        CampaignStatus.Archived  => "Архив",
        _                        => status.ToString()
    };
}
