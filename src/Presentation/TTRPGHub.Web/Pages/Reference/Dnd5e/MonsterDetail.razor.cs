using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Dnd5e;

public partial class MonsterDetail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private MonsterDetailDto? _monster;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try { _monster = await Api.GetDnd5eMonsterAsync(Id); }
        catch { _monster = null; }
        finally { _loading = false; }
    }

    private static string Modifier(int score)
    {
        var mod = (score - 10) / 2;
        return mod >= 0 ? $"+{mod}" : mod.ToString();
    }

    private static string CrBadge(string cr)
    {
        if (!double.TryParse(cr.Replace("/", "."), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var val))
            return "bg-secondary";
        return val switch
        {
            <= 0.5 => "bg-success",
            <= 4   => "bg-info text-dark",
            <= 10  => "bg-warning text-dark",
            <= 16  => "bg-danger",
            _      => "bg-dark border border-danger text-danger"
        };
    }

    private static string FormatJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return "";
        try
        {
            var items = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (items is null) return json;

            return string.Join("\n\n", items.Select(item =>
            {
                var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                var desc = item.TryGetProperty("desc", out var d) ? d.GetString() : null;
                return name is not null ? $"**{name}**\n{desc}" : desc ?? "";
            }));
        }
        catch { return json; }
    }
}
