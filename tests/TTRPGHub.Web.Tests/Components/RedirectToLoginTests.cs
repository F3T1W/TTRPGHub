using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TTRPGHub.Components.Shared;

namespace TTRPGHub.Web.Tests.Components;

public class RedirectToLoginTests : BunitContext
{
    [Fact]
    public void OnInitialized_NavigatesToLoginWithReturnUrl()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("characters/abc-123");

        Render<RedirectToLogin>();

        Assert.EndsWith("/login?returnUrl=characters%2Fabc-123", nav.Uri);
    }
}
