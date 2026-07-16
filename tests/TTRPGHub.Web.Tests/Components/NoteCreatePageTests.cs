using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Pages.Notes;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class NoteCreatePageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly Guid _campaignId = Guid.NewGuid();

    public NoteCreatePageTests()
    {
        Services.AddSingleton(_api);
    }

    private IRenderedComponent<Create> RenderPage() =>
        Render<Create>(p => p.Add(c => c.CampaignId, _campaignId));

    private static void FillValidForm(IRenderedComponent<Create> cut)
    {
        cut.Find("input.form-control").Change("Session 1 recap");
        cut.Find("textarea.form-control").Change("The party met at the tavern.");
    }

    [Fact]
    public void HandleSubmit_ValidForm_NavigatesToNewNote()
    {
        var noteId = Guid.NewGuid();
        _api.CreateNoteAsync(Arg.Any<CreateNoteRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateNoteResponse(noteId));
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = RenderPage();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.EndsWith($"/notes/{noteId}", nav.Uri);
    }

    [Fact]
    public void HandleSubmit_SendsCampaignIdFromRouteParameter()
    {
        _api.CreateNoteAsync(Arg.Any<CreateNoteRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateNoteResponse(Guid.NewGuid()));

        var cut = RenderPage();
        FillValidForm(cut);
        cut.Find("form").Submit();

        _ = _api.Received(1).CreateNoteAsync(
            Arg.Is<CreateNoteRequest>(r => r.CampaignId == _campaignId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void HandleSubmit_ApiFailure_ShowsExceptionMessage()
    {
        _api.CreateNoteAsync(Arg.Any<CreateNoteRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<CreateNoteResponse>(new InvalidOperationException("Campaign not found")));

        var cut = RenderPage();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.Contains("Campaign not found", cut.Markup);
    }

    [Fact]
    public void Render_EmptyForm_Submit_ShowsValidationErrorsWithoutCallingApi()
    {
        var cut = RenderPage();

        cut.Find("form").Submit();

        Assert.Contains("Введите заголовок", cut.Markup);
        Assert.Contains("Введите заметки", cut.Markup);
    }

    [Fact]
    public void Render_CancelLink_PointsBackToCampaign()
    {
        var cut = RenderPage();

        var cancelLink = cut.Find("a.btn-outline-light");

        Assert.Equal($"/campaigns/{_campaignId}", cancelLink.GetAttribute("href"));
    }
}
