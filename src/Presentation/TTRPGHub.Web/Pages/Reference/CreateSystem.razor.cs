using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference;

public partial class CreateSystem
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string _name = string.Empty;
    private bool _submitting;
    private string? _error;

    private async Task SubmitAsync()
    {
        _submitting = true;
        _error = null;
        try
        {
            var response = await Api.CreateGameSystemAsync(new CreateGameSystemRequest(_name));
            Nav.NavigateTo($"/reference/{response.Slug}/rules/rule");
        }
        catch
        {
            _error = "Не удалось создать систему. Попробуйте другое название.";
        }
        finally
        {
            _submitting = false;
        }
    }
}
