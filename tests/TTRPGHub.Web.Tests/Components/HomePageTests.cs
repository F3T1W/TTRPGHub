using Bunit;
using TTRPGHub.Pages;

namespace TTRPGHub.Web.Tests.Components;

public class HomePageTests : BunitContext
{
    [Fact]
    public void Render_Anonymous_ShowsGuestHeroAndLoginLinks()
    {
        var auth = AddAuthorization();
        auth.SetNotAuthorized();

        var cut = Render<Home>();

        Assert.Contains("Начать бесплатно", cut.Markup);
        Assert.Contains("/register", cut.Markup);
    }

    [Fact]
    public void Render_Authenticated_ShowsQuickActionsNotGuestHero()
    {
        var auth = AddAuthorization();
        auth.SetAuthorized("grog");

        var cut = Render<Home>();

        Assert.Contains("/characters", cut.Markup);
        Assert.DoesNotContain("Начать бесплатно", cut.Markup);
    }
}
