using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Pages.Homebrew;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class HomebrewDetailPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly IJSRuntime _js = Substitute.For<IJSRuntime>();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _authorId = Guid.NewGuid();

    public HomebrewDetailPageTests()
    {
        Services.AddSingleton(_api);
        Services.AddSingleton(new TokenStorage(_js));
    }

    private HomebrewDetailDto MakeItem(bool likedByMe = false) => new(
        _itemId, "Fireball+", "A stronger fireball", "pf2e", "Spell",
        "Deals extra damage on a critical hit.", "fire,aoe", _authorId, "grog",
        3, likedByMe, DateTime.UtcNow, null);

    private void SetStoredUserId(Guid? userId) =>
        _js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>(userId?.ToString()));

    private IRenderedComponent<Detail> RenderPage()
    {
        AddAuthorization().SetNotAuthorized();
        return Render<Detail>(p => p.Add(c => c.Id, _itemId));
    }

    [Fact]
    public void Render_ItemLoaded_ShowsTitleDescriptionAndContent()
    {
        SetStoredUserId(null);
        _api.GetHomebrewDetailAsync(_itemId, Arg.Any<CancellationToken>()).Returns(MakeItem());

        var cut = RenderPage();

        Assert.Contains("Fireball+", cut.Markup);
        Assert.Contains("A stronger fireball", cut.Markup);
        Assert.Contains("Deals extra damage on a critical hit.", cut.Markup);
        Assert.Contains("grog", cut.Markup);
    }

    [Fact]
    public void Render_NotOwner_HidesDeleteButton()
    {
        SetStoredUserId(Guid.NewGuid());
        _api.GetHomebrewDetailAsync(_itemId, Arg.Any<CancellationToken>()).Returns(MakeItem());

        var cut = RenderPage();

        Assert.Empty(cut.FindAll("button.btn-outline-danger"));
    }

    [Fact]
    public void Render_Owner_ShowsDeleteButton()
    {
        SetStoredUserId(_authorId);
        _api.GetHomebrewDetailAsync(_itemId, Arg.Any<CancellationToken>()).Returns(MakeItem());

        var cut = RenderPage();

        Assert.Single(cut.FindAll("button.btn-outline-danger"));
    }

    [Fact]
    public void Delete_ByOwner_NavigatesToHomebrewList()
    {
        SetStoredUserId(_authorId);
        _api.GetHomebrewDetailAsync(_itemId, Arg.Any<CancellationToken>()).Returns(MakeItem());
        _api.DeleteHomebrewAsync(_itemId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var cut = RenderPage();
        var nav = Services.GetRequiredService<NavigationManager>();
        cut.Find("button.btn-outline-danger").Click();

        Assert.EndsWith("/homebrew", nav.Uri);
    }

    [Fact]
    public void Render_TagsPresent_ShowsHashtagBadges()
    {
        SetStoredUserId(null);
        _api.GetHomebrewDetailAsync(_itemId, Arg.Any<CancellationToken>()).Returns(MakeItem());

        var cut = RenderPage();

        Assert.Contains("#fire", cut.Markup);
        Assert.Contains("#aoe", cut.Markup);
    }
}
