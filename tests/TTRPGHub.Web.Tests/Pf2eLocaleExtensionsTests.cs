using System.Net;
using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

file sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(respond(request));
}

public class Pf2eLocaleExtensionsTests
{
    private static Pf2eLocaleService CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var http = new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("http://localhost/") };
        var language = new ContentLanguageService(Substitute.For<IJSRuntime>());
        return new Pf2eLocaleService(http, language);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    [Fact]
    public async Task LocalizeAsync_SpellSummary_TranslatesNameAndCsvFields()
    {
        var service = CreateService(req => req.RequestUri!.AbsolutePath switch
        {
            var p when p.EndsWith("spells.ru.json") => Json("""{"acid-arrow":{"name":"Кислотная стрела"}}"""),
            var p when p.EndsWith("glossary.ru.json") => Json("""{"arcane":"аркана","attack":"атака"}"""),
            _ => Json("{}")
        });
        var spell = new Pf2eSpellSummaryDto(
            Guid.NewGuid(), "acid-arrow", "Acid Arrow", 2, "arcane", "attack",
            "2", "120 feet", "1 round");

        var localized = await service.LocalizeAsync(spell);

        Assert.Equal("Кислотная стрела", localized.Name);
        Assert.Equal("аркана", localized.Traditions);
        Assert.Equal("атака", localized.Traits);
        Assert.Equal("120 feet", localized.Range);
    }

    [Fact]
    public async Task LocalizeAsync_MonsterSummary_FallsBackToEnglishWhenNoTranslation()
    {
        var service = CreateService(_ => Json("{}"));
        var monster = new Pf2eMonsterSummaryDto(Guid.NewGuid(), "goblin-warrior", "Goblin Warrior", 1, "Small", "goblinoid", 15, 6);

        var localized = await service.LocalizeAsync(monster);

        Assert.Equal("Goblin Warrior", localized.Name);
        Assert.Equal("Small", localized.Size);
    }

    [Fact]
    public async Task LocalizeAsync_SpellDetail_UsesSeparateHeightenedField()
    {
        var service = CreateService(req => req.RequestUri!.AbsolutePath.EndsWith("spells.ru.json")
            ? Json("""{"fireball":{"name":"Огненный шар","description":"Базовое описание","heightened":"При усилении..."}}""")
            : Json("{}"));
        var spell = new Pf2eSpellDetailDto(
            Guid.NewGuid(), "fireball", "Fireball", 3, "arcane,primal", "fire,evocation",
            "2", "500 feet", "20-foot burst", null, "instantaneous",
            "A ball of fire.", "At higher levels...", "Core Rulebook", null, null, null);

        var localized = await service.LocalizeAsync(spell);

        Assert.Equal("Огненный шар", localized.Name);
        Assert.Equal("Базовое описание", localized.Description);
        Assert.Equal("При усилении...", localized.Heightened);
    }
}
