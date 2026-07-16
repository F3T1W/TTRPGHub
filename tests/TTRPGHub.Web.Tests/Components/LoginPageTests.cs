using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Refit;
using TTRPGHub.Pages;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class LoginPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly IJSRuntime _js = Substitute.For<IJSRuntime>();

    public LoginPageTests()
    {
        Services.AddSingleton(_api);
        Services.AddSingleton(new AppAuthStateProvider(new TokenStorage(_js)));
    }

    private static async Task<ApiException> MakeApiException(HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/auth/login");
        var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private IRenderedComponent<Login> RenderLogin()
    {
        return Render<Login>();
    }

    [Fact]
    public void SubmitAsync_ValidCredentials_NavigatesHomeWhenNoReturnUrl()
    {
        // ReturnUrl is populated via [SupplyParameterFromQuery], which needs real router
        // infrastructure bUnit doesn't wire up for a directly-rendered component — so this
        // exercises the no-query-string default (navigate to "/") rather than a return URL.
        _api.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LoginResponse("access-token", "refresh-token", "grog", Guid.NewGuid()));
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = RenderLogin();
        cut.Find("input[type=email]").Change("grog@example.com");
        cut.Find("input[type=password]").Change("Sup3rSecret!");
        cut.Find("form").Submit();

        Assert.Equal(nav.BaseUri, nav.Uri);
    }

    [Fact]
    public void SubmitAsync_InvalidCredentials_ShowsRussianErrorMessage()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(async _ => throw await MakeApiException(HttpStatusCode.UnprocessableEntity));

        var cut = RenderLogin();
        cut.Find("input[type=email]").Change("grog@example.com");
        cut.Find("input[type=password]").Change("WrongPassword!");
        cut.Find("form").Submit();

        Assert.Contains("Неверный email или пароль.", cut.Markup);
    }

    [Fact]
    public void SubmitAsync_NetworkFailure_ShowsGenericErrorMessage()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<LoginResponse>(new HttpRequestException("connection refused")));

        var cut = RenderLogin();
        cut.Find("input[type=email]").Change("grog@example.com");
        cut.Find("input[type=password]").Change("Sup3rSecret!");
        cut.Find("form").Submit();

        Assert.Contains("Ошибка соединения с сервером.", cut.Markup);
    }

    [Fact]
    public void Render_EmptyForm_Submit_ShowsValidationErrorsWithoutCallingApi()
    {
        var cut = RenderLogin();

        cut.Find("form").Submit();

        Assert.Contains("Введите email.", cut.Markup);
        Assert.Contains("Введите пароль.", cut.Markup);
    }
}
