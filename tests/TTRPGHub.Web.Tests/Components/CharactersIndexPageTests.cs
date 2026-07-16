using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Services;
using CharactersIndex = TTRPGHub.Pages.Characters.Index;

namespace TTRPGHub.Web.Tests.Components;

public class CharactersIndexPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public CharactersIndexPageTests()
    {
        Services.AddSingleton(_api);
    }

    private static CharacterSummaryDto MakeCharacter(string name) =>
        new(Guid.NewGuid(), name, "Human", "Fighter", 5, null, DateTime.UtcNow);

    [Fact]
    public void Render_Authenticated_WithCharacters_ShowsNameRaceClassAndLevel()
    {
        _api.GetMyCharactersAsync(Arg.Any<CancellationToken>()).Returns([MakeCharacter("Grog")]);
        var auth = AddAuthorization();
        auth.SetAuthorized("grog");

        var cut = Render<CharactersIndex>();

        Assert.Contains("Grog", cut.Markup);
        Assert.Contains("Human · Fighter", cut.Markup);
        Assert.Contains("5 ур.", cut.Markup);
    }

    [Fact]
    public void Render_Authenticated_NoCharacters_ShowsEmptyState()
    {
        _api.GetMyCharactersAsync(Arg.Any<CancellationToken>()).Returns([]);
        var auth = AddAuthorization();
        auth.SetAuthorized("grog");

        var cut = Render<CharactersIndex>();

        Assert.Contains("Пока нет персонажей", cut.Markup);
    }

    [Fact]
    public void Render_Unauthenticated_SkipsLoadingCharacters()
    {
        var auth = AddAuthorization();
        auth.SetNotAuthorized();

        var cut = Render<CharactersIndex>();

        _ = _api.DidNotReceive().GetMyCharactersAsync(Arg.Any<CancellationToken>());
        Assert.Contains("Пока нет персонажей", cut.Markup);
    }

    [Fact]
    public void Render_ApiFailure_ShowsErrorWithRetryButton()
    {
        _api.GetMyCharactersAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<List<CharacterSummaryDto>>(new HttpRequestException("boom")));
        var auth = AddAuthorization();
        auth.SetAuthorized("grog");

        var cut = Render<CharactersIndex>();

        Assert.Contains("Не удалось загрузить персонажей.", cut.Markup);
        Assert.Contains("Повторить", cut.Markup);
    }
}
