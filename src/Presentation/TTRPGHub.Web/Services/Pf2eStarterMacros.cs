using System.Text;
using Refit;

namespace TTRPGHub.Services;

internal static class Pf2eStarterMacros
{
    public const string ModulePath = "data/pf2e-starter-macros.module.json";

    public static async Task<ImportModuleResponse> ImportAsync(HttpClient http, IApiClient api, CancellationToken ct = default)
    {
        var json = await http.GetStringAsync(ModulePath, ct);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var part = new StreamPart(ms, "pf2e-starter-macros.module.json", "application/json");
        return await api.ImportModuleAsync(part, ct);
    }
}
