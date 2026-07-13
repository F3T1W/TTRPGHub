namespace TTRPGHub.Services;

public static class ApiBaseUrl
{
    private const int ApiPort = 5014;

    // WASM грузится с хоста страницы (localhost или LAN IP). Если в appsettings указан
    // localhost — подставляем тот же host, что у Web, чтобы коллега в сети бил в ваш API.
    public static string Resolve(IConfiguration configuration, string wasmBaseAddress)
    {
        var configured = configuration["ApiBaseUrl"]?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configured)
            && Uri.TryCreate(configured, UriKind.Absolute, out var uri)
            && uri.Host is not ("localhost" or "127.0.0.1"))
            return configured;

        var host = new Uri(wasmBaseAddress).Host;
        return $"http://{host}:{ApiPort}";
    }
}
