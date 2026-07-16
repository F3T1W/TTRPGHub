using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Pages.Characters;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class CharacterCreatePageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public CharacterCreatePageTests()
    {
        Services.AddSingleton(_api);
    }

    private void FillValidForm(IRenderedComponent<Create> cut)
    {
        cut.Find("input.form-control").Change("Grog");
        cut.FindAll("select.form-select")[0].Change("Человек");
        cut.FindAll("select.form-select")[1].Change("Варвар");
    }

    [Fact]
    public void SubmitAsync_ValidForm_NavigatesToNewCharacter()
    {
        var characterId = Guid.NewGuid();
        _api.CreateCharacterAsync(Arg.Any<CreateCharacterRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateCharacterResponse(characterId, "Grog", "Человек", "Варвар", 1));
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Create>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.EndsWith($"/characters/{characterId}", nav.Uri);
    }

    [Fact]
    public void SubmitAsync_ApiFailure_ShowsGenericErrorMessage()
    {
        _api.CreateCharacterAsync(Arg.Any<CreateCharacterRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<CreateCharacterResponse>(new HttpRequestException("boom")));

        var cut = Render<Create>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.Contains("Не удалось создать персонажа. Попробуй ещё раз.", cut.Markup);
    }

    [Fact]
    public void Render_EmptyForm_Submit_ShowsValidationErrorsWithoutCallingApi()
    {
        var cut = Render<Create>();

        cut.Find("form").Submit();

        Assert.Contains("Введи имя персонажа.", cut.Markup);
        Assert.Contains("Выбери расу.", cut.Markup);
        Assert.Contains("Выбери класс.", cut.Markup);
    }

    [Fact]
    public void Render_DefaultLevel_DisplaysOne()
    {
        var cut = Render<Create>();

        Assert.Equal("1", cut.Find("strong").TextContent);
    }
}
