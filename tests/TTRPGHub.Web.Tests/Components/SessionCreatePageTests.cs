using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Refit;
using TTRPGHub.Pages.Sessions;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class SessionCreatePageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly IJSRuntime _js = Substitute.For<IJSRuntime>();

    public SessionCreatePageTests()
    {
        Services.AddSingleton(_api);
        Services.AddSingleton(new TokenStorage(_js));
        // No stored user id -> OnInitializedAsync's optional city auto-fill is skipped entirely.
        _js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>((string?)null));
    }

    private static void FillRequiredFields(IRenderedComponent<Create> cut)
    {
        cut.Find("input.form-control").Change("Open table");
        cut.Find("select.form-select").Change("Pathfinder 2e");
    }

    private static async Task<ApiException> MakeApiException(System.Net.HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/sessions");
        var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    [Fact]
    public void SubmitAsync_ValidForm_NavigatesToNewSession()
    {
        var sessionId = Guid.NewGuid();
        _api.CreateSessionAsync(Arg.Any<CreateSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateSessionResponse(sessionId, "Open table"));
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Create>();
        FillRequiredFields(cut);
        cut.Find("form").Submit();

        Assert.EndsWith($"/sessions/{sessionId}", nav.Uri);
    }

    [Fact]
    public void SubmitAsync_UnprocessableEntity_ShowsFieldCheckErrorMessage()
    {
        _api.CreateSessionAsync(Arg.Any<CreateSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(async _ => throw await MakeApiException(System.Net.HttpStatusCode.UnprocessableEntity));

        var cut = Render<Create>();
        FillRequiredFields(cut);
        cut.Find("form").Submit();

        Assert.Contains("Проверьте заполненные поля.", cut.Markup);
    }

    [Fact]
    public void SubmitAsync_NetworkFailure_ShowsGenericErrorMessage()
    {
        _api.CreateSessionAsync(Arg.Any<CreateSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<CreateSessionResponse>(new HttpRequestException("boom")));

        var cut = Render<Create>();
        FillRequiredFields(cut);
        cut.Find("form").Submit();

        Assert.Contains("Ошибка при создании сессии.", cut.Markup);
    }

    [Fact]
    public void Render_EmptyForm_Submit_ShowsValidationErrorsWithoutCallingApi()
    {
        var cut = Render<Create>();

        cut.Find("form").Submit();

        Assert.Contains("Обязательное поле", cut.Markup);
        Assert.Contains("Выберите систему", cut.Markup);
    }

    [Fact]
    public void OnInitializedAsync_UserHasCityInProfile_PrefillsLocation()
    {
        var userId = Guid.NewGuid();
        _js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>(userId.ToString()));
        _api.GetUserProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserProfileDto(
            userId, "grog", null, null, "Moscow", null, "Newcomer", DateTime.UtcNow, [], []));

        var cut = Render<Create>();

        var locationInput = cut.FindAll("input.form-control")[3];
        Assert.Equal("Moscow", locationInput.GetAttribute("value"));
    }
}
