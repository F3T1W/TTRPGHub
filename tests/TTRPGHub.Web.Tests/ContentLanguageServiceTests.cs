using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class ContentLanguageServiceTests
{
    [Fact]
    public void IsRussian_DefaultsToTrueWithoutTouchingJsInterop()
    {
        var js = Substitute.For<IJSRuntime>();
        var service = new ContentLanguageService(js);

        Assert.True(service.IsRussian);
        Assert.Equal(Pf2eContentLanguage.Ru, service.Current);
    }

    [Fact]
    public async Task SetAsync_SameLanguage_DoesNotRaiseOnChanged()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>("ru"));
        var service = new ContentLanguageService(js);
        var raised = false;
        service.OnChanged += () => raised = true;

        await service.SetAsync(Pf2eContentLanguage.Ru);

        Assert.False(raised);
    }

    [Fact]
    public async Task SetAsync_DifferentLanguage_UpdatesCurrentAndRaisesOnChanged()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>("ru"));
        var service = new ContentLanguageService(js);
        var raised = false;
        service.OnChanged += () => raised = true;

        await service.SetAsync(Pf2eContentLanguage.En);

        Assert.True(raised);
        Assert.False(service.IsRussian);
        Assert.Equal(Pf2eContentLanguage.En, service.Current);
    }
}
