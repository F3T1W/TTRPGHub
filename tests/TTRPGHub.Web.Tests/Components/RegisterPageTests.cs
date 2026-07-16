using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Refit;
using TTRPGHub.Pages;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests.Components;

public class RegisterPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public RegisterPageTests()
    {
        Services.AddSingleton(_api);
    }

    private static async Task<ApiException> MakeApiException(HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/auth/register");
        var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private void FillValidForm(IRenderedComponent<Register> cut)
    {
        cut.Find("input[placeholder='tavern_master']").Change("grog123");
        cut.Find("input[type=email]").Change("grog@example.com");
        cut.Find("input[type=password]").Change("Sup3rSecret!");
    }

    [Fact]
    public void SubmitAsync_ValidRegistration_ShowsSuccessMessage()
    {
        _api.RegisterAsync(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterResponse(Guid.NewGuid(), "grog123", "grog@example.com"));

        var cut = Render<Register>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.Contains("Аккаунт создан!", cut.Markup);
    }

    [Fact]
    public void SubmitAsync_DuplicateEmail_ShowsConflictErrorMessage()
    {
        _api.RegisterAsync(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>())
            .Returns(async _ => throw await MakeApiException(HttpStatusCode.Conflict));

        var cut = Render<Register>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.Contains("Пользователь с таким email уже существует.", cut.Markup);
        Assert.DoesNotContain("Аккаунт создан!", cut.Markup);
    }

    [Fact]
    public void SubmitAsync_NetworkFailure_ShowsGenericErrorMessage()
    {
        _api.RegisterAsync(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<RegisterResponse>(new HttpRequestException("connection refused")));

        var cut = Render<Register>();
        FillValidForm(cut);
        cut.Find("form").Submit();

        Assert.Contains("Ошибка соединения с сервером.", cut.Markup);
    }

    [Fact]
    public void Render_EmptyForm_Submit_ShowsValidationErrorsWithoutCallingApi()
    {
        var cut = Render<Register>();

        cut.Find("form").Submit();

        Assert.Contains("Введите имя пользователя.", cut.Markup);
        Assert.Contains("Введите email.", cut.Markup);
        Assert.Contains("Введите пароль.", cut.Markup);
    }

    [Fact]
    public void Render_ShortPassword_ShowsValidationError()
    {
        var cut = Render<Register>();
        cut.Find("input[placeholder='tavern_master']").Change("grog123");
        cut.Find("input[type=email]").Change("grog@example.com");
        cut.Find("input[type=password]").Change("short");

        cut.Find("form").Submit();

        Assert.Contains("Минимум 8 символов.", cut.Markup);
    }
}
