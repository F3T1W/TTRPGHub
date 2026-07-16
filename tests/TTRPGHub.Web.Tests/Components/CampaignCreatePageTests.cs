using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Pages.Campaigns;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class CampaignCreatePageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public CampaignCreatePageTests()
    {
        Services.AddSingleton(_api);
    }

    private static void FillValidForm(IRenderedComponent<Create> cut)
    {
        cut.FindAll("input.form-control")[0].Change("The Beginning");
        cut.FindAll("input.form-control")[1].Change("pf2e");
    }

    [Fact]
    public void HandleSubmit_ValidForm_NavigatesToNewCampaign()
    {
        var campaignId = Guid.NewGuid();
        _api.CreateCampaignAsync(Arg.Any<CreateCampaignRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateCampaignResponse(campaignId, "The Beginning"));
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Create>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.EndsWith($"/campaigns/{campaignId}", nav.Uri);
    }

    [Fact]
    public void HandleSubmit_ApiFailure_ShowsExceptionMessage()
    {
        _api.CreateCampaignAsync(Arg.Any<CreateCampaignRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<CreateCampaignResponse>(new InvalidOperationException("Server unavailable")));

        var cut = Render<Create>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.Contains("Server unavailable", cut.Markup);
    }

    [Fact]
    public void Render_EmptyForm_Submit_ShowsValidationErrorsWithoutCallingApi()
    {
        var cut = Render<Create>();

        cut.Find("form").Submit();

        Assert.Contains("Введите название", cut.Markup);
        Assert.Contains("Укажите систему", cut.Markup);
    }

    [Fact]
    public void HandleSubmit_DescriptionIsOptional_SubmitsSuccessfully()
    {
        var campaignId = Guid.NewGuid();
        _api.CreateCampaignAsync(Arg.Is<CreateCampaignRequest>(r => r.Description == null), Arg.Any<CancellationToken>())
            .Returns(new CreateCampaignResponse(campaignId, "The Beginning"));
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Create>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.EndsWith($"/campaigns/{campaignId}", nav.Uri);
    }
}
