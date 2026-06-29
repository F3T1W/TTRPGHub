using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages;

public partial class Profile
{
    [Inject] private TokenStorage Tokens { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var userId = await Tokens.GetUserIdAsync();
        Nav.NavigateTo(userId.HasValue ? $"/users/{userId}" : "/", replace: true);
    }
}
