using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Services;
using EventsIndex = TTRPGHub.Pages.Events.Index;

namespace TTRPGHub.Web.Tests.Components;

public class EventsIndexPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public EventsIndexPageTests()
    {
        Services.AddSingleton(_api);
        AddAuthorization().SetNotAuthorized();
    }

    private static GameEventSummaryDto MakeEvent(string title) => new(
        Guid.NewGuid(), title, "pf2e", "Online", null, "https://discord.gg/test",
        DateTime.UtcNow.AddDays(3), 6, 2, Guid.NewGuid(), "gm_grog", false);

    [Fact]
    public void Render_WithEvents_ShowsTitleFormatAndParticipantCount()
    {
        _api.GetEventsAsync(1, 12, null, null, Arg.Any<CancellationToken>())
            .Returns(new EventsPagedResult([MakeEvent("Open Table")], 1, 1, 12));

        var cut = Render<EventsIndex>();

        Assert.Contains("Open Table", cut.Markup);
        Assert.Contains("Онлайн", cut.Markup);
        Assert.Contains("2 / 6", cut.Markup);
    }

    [Fact]
    public void Render_NoEvents_ShowsEmptyState()
    {
        _api.GetEventsAsync(1, 12, null, null, Arg.Any<CancellationToken>())
            .Returns(new EventsPagedResult([], 0, 1, 12));

        var cut = Render<EventsIndex>();

        Assert.Contains("Предстоящих событий нет.", cut.Markup);
    }

    [Fact]
    public void Render_SinglePage_HidesPaginationControls()
    {
        _api.GetEventsAsync(1, 12, null, null, Arg.Any<CancellationToken>())
            .Returns(new EventsPagedResult([MakeEvent("Open Table")], 1, 1, 12));

        var cut = Render<EventsIndex>();

        Assert.DoesNotContain("1 / 1", cut.Markup);
    }

    [Fact]
    public void NextPage_MultiplePages_RequestsPageTwo()
    {
        _api.GetEventsAsync(1, 12, null, null, Arg.Any<CancellationToken>())
            .Returns(new EventsPagedResult([MakeEvent("Page 1 event")], 24, 1, 12));
        _api.GetEventsAsync(2, 12, null, null, Arg.Any<CancellationToken>())
            .Returns(new EventsPagedResult([MakeEvent("Page 2 event")], 24, 2, 12));

        var cut = Render<EventsIndex>();
        cut.Find("button.btn-sm.btn-outline-secondary:not([disabled])").Click();

        Assert.Contains("Page 2 event", cut.Markup);
        Assert.Contains("2 / 2", cut.Markup);
    }

    [Fact]
    public void ApplyFilters_TypedLocation_IsForwardedToApi()
    {
        _api.GetEventsAsync(1, 12, null, null, Arg.Any<CancellationToken>())
            .Returns(new EventsPagedResult([], 0, 1, 12));
        _api.GetEventsAsync(1, 12, "Moscow", null, Arg.Any<CancellationToken>())
            .Returns(new EventsPagedResult([MakeEvent("Moscow Meetup")], 1, 1, 12));

        var cut = Render<EventsIndex>();
        cut.Find("input.form-control").Input("Moscow");
        cut.Find("button.btn-ta-primary").Click();

        Assert.Contains("Moscow Meetup", cut.Markup);
    }
}
