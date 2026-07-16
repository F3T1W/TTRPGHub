using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Services;
using HomebrewIndex = TTRPGHub.Pages.Homebrew.Index;

namespace TTRPGHub.Web.Tests.Components;

public class HomebrewIndexPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public HomebrewIndexPageTests()
    {
        Services.AddSingleton(_api);
        AddAuthorization().SetNotAuthorized();
    }

    private static HomebrewItemDto MakeItem(string title) => new(
        Guid.NewGuid(), title, "A homebrew spell.", "pf2e", "Spell",
        "fire,aoe", Guid.NewGuid(), "grog", 3, false, DateTime.UtcNow);

    [Fact]
    public void Render_WithItems_ShowsTitleTypeAndLikeCount()
    {
        _api.SearchHomebrewAsync(null, null, null, null, 1, 12, Arg.Any<CancellationToken>())
            .Returns(new HomebrewPagedResult([MakeItem("Fireball+")], 1, 1, 12));

        var cut = Render<HomebrewIndex>();

        Assert.Contains("Fireball+", cut.Markup);
        Assert.Contains("Заклинание", cut.Markup);
        Assert.Contains("grog", cut.Markup);
    }

    [Fact]
    public void Render_NoItems_ShowsEmptyState()
    {
        _api.SearchHomebrewAsync(null, null, null, null, 1, 12, Arg.Any<CancellationToken>())
            .Returns(new HomebrewPagedResult([], 0, 1, 12));

        var cut = Render<HomebrewIndex>();

        Assert.Contains("Материалов пока нет. Будь первым!", cut.Markup);
    }

    [Fact]
    public void Render_SinglePage_HidesPageButtons()
    {
        _api.SearchHomebrewAsync(null, null, null, null, 1, 12, Arg.Any<CancellationToken>())
            .Returns(new HomebrewPagedResult([MakeItem("Fireball+")], 1, 1, 12));

        var cut = Render<HomebrewIndex>();

        Assert.Empty(cut.FindAll("button.btn-sm"));
    }

    [Fact]
    public void GoToPageAsync_ClickingPageTwo_RequestsPageTwo()
    {
        _api.SearchHomebrewAsync(null, null, null, null, 1, 12, Arg.Any<CancellationToken>())
            .Returns(new HomebrewPagedResult([MakeItem("Page 1 item")], 24, 1, 12));
        _api.SearchHomebrewAsync(null, null, null, null, 2, 12, Arg.Any<CancellationToken>())
            .Returns(new HomebrewPagedResult([MakeItem("Page 2 item")], 24, 2, 12));

        var cut = Render<HomebrewIndex>();
        cut.FindAll("button.btn-sm")[1].Click();

        Assert.Contains("Page 2 item", cut.Markup);
    }

    [Fact]
    public void LoadAsync_SystemFilterChanged_ForwardsSystemToApi()
    {
        _api.SearchHomebrewAsync(null, null, null, null, 1, 12, Arg.Any<CancellationToken>())
            .Returns(new HomebrewPagedResult([], 0, 1, 12));
        _api.SearchHomebrewAsync(null, "Pathfinder 2e", null, null, 1, 12, Arg.Any<CancellationToken>())
            .Returns(new HomebrewPagedResult([MakeItem("PF2e Item")], 1, 1, 12));

        var cut = Render<HomebrewIndex>();
        cut.FindAll("select.form-select")[0].Change("Pathfinder 2e");

        Assert.Contains("PF2e Item", cut.Markup);
    }
}
