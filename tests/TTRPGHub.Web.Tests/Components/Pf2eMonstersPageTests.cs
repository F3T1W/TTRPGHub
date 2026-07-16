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

public class Pf2eMonstersPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public Pf2eMonstersPageTests()
    {
        Services.AddSingleton(_api);
        var http = new HttpClient(new NotFoundHandler()) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(http);
        Services.AddSingleton(new ContentLanguageService(Substitute.For<IJSRuntime>()));
        Services.AddSingleton(sp => new Pf2eLocaleService(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<ContentLanguageService>()));
    }

    private static Pf2eMonsterSummaryDto MakeMonster(string name, int level = 3) =>
        new(Guid.NewGuid(), "goblin-warrior", name, level, "Small", "goblinoid,humanoid", 18, 16);

    [Fact]
    public void Render_WithMonsters_ShowsNameLevelAndStats()
    {
        _api.GetPf2eMonstersAsync(null, null, null, null, 1, 40, Arg.Any<CancellationToken>())
            .Returns(new Pf2eMonsterPagedResult([MakeMonster("Goblin Warrior")], 1, 1, 40, 1));

        var cut = Render<Monsters>();

        Assert.Contains("Goblin Warrior", cut.Markup);
        Assert.Contains("Найдено:", cut.Markup);
        Assert.Contains("18", cut.Markup);
        Assert.Contains("16", cut.Markup);
    }

    [Fact]
    public void Render_NoResults_ShowsNotFoundMessage()
    {
        _api.GetPf2eMonstersAsync(null, null, null, null, 1, 40, Arg.Any<CancellationToken>())
            .Returns(new Pf2eMonsterPagedResult([], 0, 1, 40, 0));

        var cut = Render<Monsters>();

        Assert.Contains("Существа не найдены. Попробуй изменить фильтры.", cut.Markup);
    }

    [Fact]
    public void Render_SinglePage_HidesPaginationControls()
    {
        _api.GetPf2eMonstersAsync(null, null, null, null, 1, 40, Arg.Any<CancellationToken>())
            .Returns(new Pf2eMonsterPagedResult([MakeMonster("Goblin Warrior")], 1, 1, 40, 1));

        var cut = Render<Monsters>();

        Assert.DoesNotContain("Вперёд →", cut.Markup);
    }

    [Fact]
    public void ApplyFilters_SearchText_IsForwardedToApi()
    {
        _api.GetPf2eMonstersAsync(null, null, null, null, 1, 40, Arg.Any<CancellationToken>())
            .Returns(new Pf2eMonsterPagedResult([], 0, 1, 40, 0));
        _api.GetPf2eMonstersAsync("dragon", null, null, null, 1, 40, Arg.Any<CancellationToken>())
            .Returns(new Pf2eMonsterPagedResult([MakeMonster("Young Red Dragon")], 1, 1, 40, 1));

        var cut = Render<Monsters>();
        cut.Find("input.form-control").Input("dragon");
        cut.Find("button.btn-ta-primary.flex-grow-1").Click();

        Assert.Contains("Young Red Dragon", cut.Markup);
    }

    [Fact]
    public void NextPage_MultiplePages_RequestsPageTwo()
    {
        _api.GetPf2eMonstersAsync(null, null, null, null, 1, 40, Arg.Any<CancellationToken>())
            .Returns(new Pf2eMonsterPagedResult([MakeMonster("Page 1 Monster")], 80, 1, 40, 2));
        _api.GetPf2eMonstersAsync(null, null, null, null, 2, 40, Arg.Any<CancellationToken>())
            .Returns(new Pf2eMonsterPagedResult([MakeMonster("Page 2 Monster")], 80, 2, 40, 2));

        var cut = Render<Monsters>();
        cut.Find("button.btn-outline-secondary.btn-sm:not([disabled])").Click();

        Assert.Contains("Page 2 Monster", cut.Markup);
    }
}
