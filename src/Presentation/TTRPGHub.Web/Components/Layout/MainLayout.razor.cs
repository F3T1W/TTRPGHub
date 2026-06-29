namespace TTRPGHub.Components.Layout;

public partial class MainLayout
{
    private async Task LogoutAsync()
    {
        await AuthProvider.NotifyLogoutAsync();
        Nav.NavigateTo("/login");
    }
}
