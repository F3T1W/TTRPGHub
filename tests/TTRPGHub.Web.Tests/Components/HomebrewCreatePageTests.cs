using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Pages.Homebrew;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class HomebrewCreatePageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public HomebrewCreatePageTests()
    {
        Services.AddSingleton(_api);
    }

    private static void FillRequiredFields(IRenderedComponent<Create> cut)
    {
        cut.FindAll("input.form-control")[0].Change("Fireball+");
        cut.FindAll("input.form-control")[1].Change("A stronger fireball");
        cut.FindAll("select.form-select")[0].Change("D&D 5e");
        cut.FindAll("textarea.form-control")[0].Change("Deals extra damage.");
    }

    [Fact]
    public void SubmitAsync_ValidForm_NavigatesToNewHomebrewItem()
    {
        var itemId = Guid.NewGuid();
        _api.CreateHomebrewAsync(Arg.Any<CreateHomebrewRequest>(), Arg.Any<CancellationToken>()).Returns(itemId);
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Create>();
        FillRequiredFields(cut);
        cut.Find("button.btn-ta-primary").Click();

        Assert.EndsWith($"/homebrew/{itemId}", nav.Uri);
    }

    [Fact]
    public void SubmitAsync_MissingRequiredFields_ShowsErrorWithoutCallingApi()
    {
        var cut = Render<Create>();

        cut.Find("button.btn-ta-primary").Click();

        Assert.Contains("Заполни все обязательные поля", cut.Markup);
        _ = _api.DidNotReceive().CreateHomebrewAsync(Arg.Any<CreateHomebrewRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SubmitAsync_ApiFailure_ShowsGenericErrorMessage()
    {
        _api.CreateHomebrewAsync(Arg.Any<CreateHomebrewRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<Guid>(new HttpRequestException("boom")));

        var cut = Render<Create>();
        FillRequiredFields(cut);
        cut.Find("button.btn-ta-primary").Click();

        Assert.Contains("Не удалось создать материал", cut.Markup);
    }

    [Fact]
    public void Render_TypeOptions_ShowRussianLabels()
    {
        var cut = Render<Create>();

        var typeSelect = cut.FindAll("select.form-select")[1];
        var optionLabels = typeSelect.QuerySelectorAll("option").Select(o => o.TextContent).ToList();

        Assert.Contains("Заклинание", optionLabels);
        Assert.Contains("Существо", optionLabels);
        Assert.Contains("Другое", optionLabels);
    }
}
