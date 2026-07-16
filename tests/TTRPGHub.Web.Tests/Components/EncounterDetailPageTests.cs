using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Pages.Encounters;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class EncounterDetailPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly Guid _encounterId = Guid.NewGuid();

    public EncounterDetailPageTests()
    {
        Services.AddSingleton(_api);
    }

    private static EncounterDetailDto MakeEncounter(bool isCreator = true) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "Goblin Ambush", "A group of goblins attacks from the trees.",
        EncounterDifficulty.Medium, "Remember the trap on the north path.",
        [new EncounterEntryDetail("Goblin Warrior", 3, "Leader has a shortbow")],
        isCreator, DateTime.UtcNow, DateTime.UtcNow);

    private IRenderedComponent<Detail> RenderPage() =>
        Render<Detail>(p => p.Add(c => c.Id, _encounterId));

    [Fact]
    public void Render_EncounterLoaded_ShowsTitleDifficultyAndEntries()
    {
        _api.GetEncounterDetailAsync(_encounterId, Arg.Any<CancellationToken>()).Returns(MakeEncounter());

        var cut = RenderPage();

        Assert.Contains("Goblin Ambush", cut.Markup);
        Assert.Contains("Среднее", cut.Markup);
        Assert.Contains("Goblin Warrior", cut.Markup);
        Assert.Contains("×3", cut.Markup);
    }

    [Fact]
    public void Render_EncounterNotFound_ShowsNotFoundMessage()
    {
        _api.GetEncounterDetailAsync(_encounterId, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<EncounterDetailDto>(new InvalidOperationException("404")));

        var cut = RenderPage();

        Assert.Contains("Столкновение не найдено.", cut.Markup);
    }

    [Fact]
    public void Render_IsCreator_ShowsEditAndDeleteButtons()
    {
        _api.GetEncounterDetailAsync(_encounterId, Arg.Any<CancellationToken>()).Returns(MakeEncounter(isCreator: true));

        var cut = RenderPage();

        Assert.Contains("Редактировать", cut.Markup);
        Assert.Contains("Удалить", cut.Markup);
    }

    [Fact]
    public void Render_NotCreator_HidesEditAndDeleteButtons()
    {
        _api.GetEncounterDetailAsync(_encounterId, Arg.Any<CancellationToken>()).Returns(MakeEncounter(isCreator: false));

        var cut = RenderPage();

        Assert.DoesNotContain("Редактировать", cut.Markup);
        Assert.DoesNotContain("Удалить", cut.Markup);
    }

    [Fact]
    public void Delete_Success_NavigatesToCampaign()
    {
        var encounter = MakeEncounter();
        _api.GetEncounterDetailAsync(_encounterId, Arg.Any<CancellationToken>()).Returns(encounter);
        _api.DeleteEncounterAsync(_encounterId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = RenderPage();
        cut.Find("button.btn-outline-danger").Click();

        Assert.EndsWith($"/campaigns/{encounter.CampaignId}", nav.Uri);
    }

    [Fact]
    public void Delete_Failure_ShowsExceptionMessage()
    {
        _api.GetEncounterDetailAsync(_encounterId, Arg.Any<CancellationToken>()).Returns(MakeEncounter());
        _api.DeleteEncounterAsync(_encounterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Only the creator can delete this encounter.")));

        var cut = RenderPage();
        cut.Find("button.btn-outline-danger").Click();

        Assert.Contains("Only the creator can delete this encounter.", cut.Markup);
    }

    [Fact]
    public void Render_NoEntries_ShowsEmptyStateMessage()
    {
        var encounter = MakeEncounter() with { Entries = [] };
        _api.GetEncounterDetailAsync(_encounterId, Arg.Any<CancellationToken>()).Returns(encounter);

        var cut = RenderPage();

        Assert.Contains("Участники не добавлены.", cut.Markup);
    }
}
