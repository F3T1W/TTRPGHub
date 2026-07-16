using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Pages.Reference.Pf2e;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

file sealed class NotFoundHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
}

public class Pf2eSpellDetailPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly Guid _spellId = Guid.NewGuid();

    public Pf2eSpellDetailPageTests()
    {
        Services.AddSingleton(_api);
        // No RU overlay files available in tests -> Pf2eLocaleService falls back to the English
        // source text for every field, which is what these assertions check against.
        var http = new HttpClient(new NotFoundHandler()) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(http);
        Services.AddSingleton(new ContentLanguageService(Substitute.For<IJSRuntime>()));
        Services.AddSingleton(sp => new Pf2eLocaleService(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<ContentLanguageService>()));
    }

    private static Pf2eSpellDetailDto MakeSpell(Guid id, int level = 3, string? heightened = "Deals +1d6 per additional slot.") => new(
        id, "fireball", "Fireball", level, "arcane,primal", "fire,evocation",
        "2", "500 feet", "20-foot burst", null, "instantaneous",
        "A roaring ball of fire explodes.", heightened, "Core Rulebook", null, null, null);

    private IRenderedComponent<SpellDetail> RenderPage(Guid id) =>
        Render<SpellDetail>(p => p.Add(c => c.Id, id));

    [Fact]
    public void Render_SpellFound_ShowsNameLevelAndDescription()
    {
        _api.GetPf2eSpellAsync(_spellId, Arg.Any<CancellationToken>()).Returns(MakeSpell(_spellId));

        var cut = RenderPage(_spellId);

        Assert.Contains("Fireball", cut.Markup);
        Assert.Contains("Заклинание 3-го уровня", cut.Markup);
        Assert.Contains("A roaring ball of fire explodes.", cut.Markup);
        Assert.Contains("500 feet", cut.Markup);
    }

    [Fact]
    public void Render_CantripLevel_ShowsCantripLabel()
    {
        _api.GetPf2eSpellAsync(_spellId, Arg.Any<CancellationToken>()).Returns(MakeSpell(_spellId, level: 0));

        var cut = RenderPage(_spellId);

        Assert.Contains("Заговор", cut.Markup);
    }

    [Fact]
    public void Render_HasHeightenedText_ShowsHeightenedSection()
    {
        _api.GetPf2eSpellAsync(_spellId, Arg.Any<CancellationToken>())
            .Returns(MakeSpell(_spellId, heightened: "Deals +2d6 per slot."));

        var cut = RenderPage(_spellId);

        Assert.Contains("Усиление (heightened)", cut.Markup);
        Assert.Contains("Deals +2d6 per slot.", cut.Markup);
    }

    [Fact]
    public void Render_NoHeightenedText_HidesHeightenedSection()
    {
        _api.GetPf2eSpellAsync(_spellId, Arg.Any<CancellationToken>())
            .Returns(MakeSpell(_spellId, heightened: null));

        var cut = RenderPage(_spellId);

        Assert.DoesNotContain("Усиление (heightened)", cut.Markup);
    }

    [Fact]
    public void Render_SpellNotFound_ShowsNotFoundMessage()
    {
        _api.GetPf2eSpellAsync(_spellId, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<Pf2eSpellDetailDto>(new InvalidOperationException("404")));

        var cut = RenderPage(_spellId);

        Assert.Contains("Заклинание не найдено.", cut.Markup);
    }
}
