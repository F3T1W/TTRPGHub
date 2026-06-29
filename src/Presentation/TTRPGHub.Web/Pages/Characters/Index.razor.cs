using TTRPGHub.Services;

namespace TTRPGHub.Pages.Characters;

public partial class Index
{
    private List<CharacterSummaryDto> _characters = [];
    private string? _error;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            await LoadAsync();
        else
            _loading = false;
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error   = null;

        try
        {
            _characters = await Api.GetMyCharactersAsync();
        }
        catch
        {
            _error = "Не удалось загрузить персонажей.";
        }
        finally
        {
            _loading = false;
        }
    }
}
