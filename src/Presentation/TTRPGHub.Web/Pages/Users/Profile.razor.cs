using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Users;

public partial class Profile
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private UserProfileDto? _profile;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try { _profile = await Api.GetUserProfileAsync(Id); }
        catch { _profile = null; }
        finally { _loading = false; }
    }
}
