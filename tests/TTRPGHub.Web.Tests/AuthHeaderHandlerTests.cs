using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

file sealed class RecordingHandler : DelegatingHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}

public class AuthHeaderHandlerTests
{
    [Fact]
    public async Task SendAsync_TokenPresent_AttachesBearerHeader()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>("my-token"));
        var recorder = new RecordingHandler();
        var handler = new AuthHeaderHandler(new TokenStorage(js)) { InnerHandler = recorder };
        var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/api/whatever");

        Assert.NotNull(recorder.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", recorder.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("my-token", recorder.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_NoToken_DoesNotAttachAuthorizationHeader()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>((string?)null));
        var recorder = new RecordingHandler();
        var handler = new AuthHeaderHandler(new TokenStorage(js)) { InnerHandler = recorder };
        var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/api/whatever");

        Assert.Null(recorder.LastRequest!.Headers.Authorization);
    }
}
