using System.Net;
using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

file sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(respond(request));
}

public class Pf2eLocaleServiceTests
{
    private static Pf2eLocaleService CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHttpMessageHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        // ContentLanguageService.Current defaults to Ru without needing JS interop to be called.
        var language = new ContentLanguageService(Substitute.For<IJSRuntime>());
        return new Pf2eLocaleService(http, language);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };

    [Fact]
    public async Task NameAsync_TranslationExists_ReturnsTranslatedName()
    {
        var service = CreateService(req => req.RequestUri!.AbsolutePath.EndsWith("spells.ru.json")
            ? JsonResponse("""{"fireball":{"name":"Огненный шар"}}""")
            : JsonResponse("{}"));

        var name = await service.NameAsync("spell", "fireball", "Fireball");

        Assert.Equal("Огненный шар", name);
    }

    [Fact]
    public async Task NameAsync_NoTranslation_FallsBackToEnglish()
    {
        var service = CreateService(_ => JsonResponse("{}"));

        var name = await service.NameAsync("spell", "unknown-spell", "Unknown Spell");

        Assert.Equal("Unknown Spell", name);
    }

    [Fact]
    public async Task DescriptionAsync_UsesHeightenedField_NotDescriptionForHeightened()
    {
        var service = CreateService(req => req.RequestUri!.AbsolutePath.EndsWith("spells.ru.json")
            ? JsonResponse("""{"fireball":{"description":"Базовое описание","heightened":"Текст усиления"}}""")
            : JsonResponse("{}"));

        var description = await service.DescriptionAsync("spell", "fireball", "fallback");
        var heightened = await service.HeightenedAsync("spell", "fireball", "fallback");

        Assert.Equal("Базовое описание", description);
        Assert.Equal("Текст усиления", heightened);
    }

    [Fact]
    public async Task LocalizeCsvAsync_UnknownTerm_ReturnsOriginalToken()
    {
        var service = CreateService(req => req.RequestUri!.AbsolutePath.EndsWith("glossary.ru.json")
            ? JsonResponse("""{"fire":"огонь"}""")
            : JsonResponse("{}"));

        var localized = await service.LocalizeCsvAsync("fire, unknown-trait");

        Assert.Equal("огонь, unknown-trait", localized);
    }

    [Fact]
    public async Task LocalizeCsvAsync_NullOrEmpty_ReturnsEmptyString()
    {
        var service = CreateService(_ => JsonResponse("{}"));

        Assert.Equal("", await service.LocalizeCsvAsync(null));
        Assert.Equal("", await service.LocalizeCsvAsync(""));
    }

    [Fact]
    public async Task LoadEntryMapAsync_HttpFailure_FallsBackToEmptyMapWithoutThrowing()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var name = await service.NameAsync("spell", "fireball", "Fireball");

        Assert.Equal("Fireball", name);
    }

    [Fact]
    public async Task HasTranslation_BeforeLoad_ReturnsFalse()
    {
        var service = CreateService(_ => JsonResponse("{}"));

        Assert.False(service.HasTranslation("spell", "fireball"));
    }

    [Fact]
    public async Task HasTranslation_AfterLoadWithEntry_ReturnsTrue()
    {
        var service = CreateService(req => req.RequestUri!.AbsolutePath.EndsWith("spells.ru.json")
            ? JsonResponse("""{"fireball":{"name":"Огненный шар"}}""")
            : JsonResponse("{}"));

        await service.EnsureLoadedAsync();

        Assert.True(service.HasTranslation("spell", "fireball"));
        Assert.False(service.HasTranslation("spell", "acid-arrow"));
    }
}
