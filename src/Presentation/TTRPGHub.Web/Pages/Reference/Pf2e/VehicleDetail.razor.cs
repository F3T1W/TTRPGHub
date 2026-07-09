using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

public partial class VehicleDetail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private Pf2eVehicleDetailDto? _vehicle;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try { _vehicle = await Api.GetPf2eVehicleAsync(Id); }
        catch { _vehicle = null; }
        finally { _loading = false; }
    }

    private static string LevelBadge(int level) => level switch
    {
        <= 0 => "bg-success",
        <= 3 => "bg-info text-dark",
        <= 6 => "bg-warning text-dark",
        <= 10 => "bg-danger",
        _ => "bg-dark border border-danger text-danger"
    };
}
