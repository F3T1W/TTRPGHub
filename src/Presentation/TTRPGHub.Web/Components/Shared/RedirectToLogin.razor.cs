namespace TTRPGHub.Components.Shared;

public partial class RedirectToLogin
{
    protected override void OnInitialized()
    {
        var returnUrl = Uri.EscapeDataString(Nav.ToBaseRelativePath(Nav.Uri));
        Nav.NavigateTo($"/login?returnUrl={returnUrl}");
    }
}
