namespace TTRPGHub.Web.Services;

public sealed class AuthHeaderHandler(TokenStorage tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokens.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}
