using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Events;

[Authorize]
public partial class Create
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string _title = string.Empty;
    private string _system = string.Empty;
    private string _format = "Online";
    private string? _location;
    private string? _onlineLink;
    private string? _description;
    private DateTime _startsAt = DateTime.Now.AddDays(7);
    private int _maxParticipants = 6;
    private bool _submitting;
    private string? _error;

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(_title) || string.IsNullOrWhiteSpace(_system))
        {
            _error = "Заполните название и систему.";
            return;
        }

        _submitting = true;
        _error = null;
        try
        {
            var id = await Api.CreateEventAsync(new CreateEventRequest(
                _title, _description, _system, _format,
                _location, _onlineLink, _startsAt.ToUniversalTime(), _maxParticipants));
            Nav.NavigateTo($"/events/{id}");
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _submitting = false; }
    }
}
