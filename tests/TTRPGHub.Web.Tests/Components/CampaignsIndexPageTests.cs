using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Services;
using CampaignsIndex = TTRPGHub.Pages.Campaigns.Index;

namespace TTRPGHub.Web.Tests.Components;

public class CampaignsIndexPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public CampaignsIndexPageTests()
    {
        Services.AddSingleton(_api);
    }

    private static CampaignSummaryDto MakeCampaign(string title, CampaignStatus status = CampaignStatus.Active, bool isOrganizer = false) =>
        new(Guid.NewGuid(), title, "A grand adventure", "pf2e", status, 4, isOrganizer, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public void Render_NoCampaigns_ShowsEmptyState()
    {
        _api.GetMyCampaignsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<CampaignsIndex>();

        Assert.Contains("У вас пока нет кампаний. Создайте первую!", cut.Markup);
    }

    [Fact]
    public void Render_WithCampaigns_ShowsTitlesAndStatusLabels()
    {
        _api.GetMyCampaignsAsync(Arg.Any<CancellationToken>()).Returns([
            MakeCampaign("The Beginning", CampaignStatus.Active),
            MakeCampaign("Old Saga", CampaignStatus.Archived),
        ]);

        var cut = Render<CampaignsIndex>();

        Assert.Contains("The Beginning", cut.Markup);
        Assert.Contains("Активна", cut.Markup);
        Assert.Contains("Old Saga", cut.Markup);
        Assert.Contains("Архив", cut.Markup);
    }

    [Fact]
    public void Render_OrganizerCampaign_ShowsOrganizerBadge()
    {
        _api.GetMyCampaignsAsync(Arg.Any<CancellationToken>()).Returns([
            MakeCampaign("My Campaign", isOrganizer: true),
        ]);

        var cut = Render<CampaignsIndex>();

        Assert.Contains("Организатор", cut.Markup);
    }

    [Fact]
    public void Render_NonOrganizerCampaign_HidesOrganizerBadge()
    {
        _api.GetMyCampaignsAsync(Arg.Any<CancellationToken>()).Returns([
            MakeCampaign("Someone Else's Campaign", isOrganizer: false),
        ]);

        var cut = Render<CampaignsIndex>();

        Assert.DoesNotContain("Организатор", cut.Markup);
    }

    [Fact]
    public void Render_CampaignWithoutDescription_ShowsPlaceholderText()
    {
        var campaign = MakeCampaign("No Description") with { Description = null };
        _api.GetMyCampaignsAsync(Arg.Any<CancellationToken>()).Returns([campaign]);

        var cut = Render<CampaignsIndex>();

        Assert.Contains("Описание отсутствует", cut.Markup);
    }
}
