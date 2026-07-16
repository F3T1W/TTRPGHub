using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Pages.Events;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class EventCreatePageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public EventCreatePageTests()
    {
        Services.AddSingleton(_api);
    }

    private static void FillRequiredFields(IRenderedComponent<Create> cut)
    {
        cut.FindAll("input.form-control")[0].Change("Open table");
        cut.FindAll("input.form-control")[1].Change("pf2e");
    }

    [Fact]
    public void SubmitAsync_ValidForm_NavigatesToNewEvent()
    {
        var eventId = Guid.NewGuid();
        _api.CreateEventAsync(Arg.Any<CreateEventRequest>(), Arg.Any<CancellationToken>()).Returns(eventId);
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Create>();
        FillRequiredFields(cut);
        cut.Find("button.btn-ta-primary").Click();

        Assert.EndsWith($"/events/{eventId}", nav.Uri);
    }

    [Fact]
    public void SubmitAsync_MissingTitleOrSystem_ShowsErrorWithoutCallingApi()
    {
        var cut = Render<Create>();

        cut.Find("button.btn-ta-primary").Click();

        Assert.Contains("Заполните название и систему.", cut.Markup);
        _ = _api.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SubmitAsync_ApiFailure_ShowsExceptionMessage()
    {
        _api.CreateEventAsync(Arg.Any<CreateEventRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<Guid>(new InvalidOperationException("Слишком много участников")));

        var cut = Render<Create>();
        FillRequiredFields(cut);
        cut.Find("button.btn-ta-primary").Click();

        Assert.Contains("Слишком много участников", cut.Markup);
    }

    [Fact]
    public void Render_DefaultFormat_ShowsOnlineLinkNotLocation()
    {
        var cut = Render<Create>();

        Assert.Contains("Ссылка на онлайн", cut.Markup);
        Assert.DoesNotContain("Место проведения", cut.Markup);
    }

    [Fact]
    public void Render_OfflineFormat_ShowsLocationNotOnlineLink()
    {
        var cut = Render<Create>();

        cut.Find("select.form-select").Change("Offline");

        Assert.Contains("Место проведения", cut.Markup);
        Assert.DoesNotContain("Ссылка на онлайн", cut.Markup);
    }
}
