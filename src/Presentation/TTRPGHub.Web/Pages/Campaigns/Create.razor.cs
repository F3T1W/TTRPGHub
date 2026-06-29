using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Campaigns;

public partial class Create
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private readonly FormModel _model = new();
    private bool _saving;
    private string? _error;

    private async Task HandleSubmit()
    {
        _saving = true;
        _error = null;
        try
        {
            var response = await Api.CreateCampaignAsync(
                new CreateCampaignRequest(_model.Title, _model.Description, _model.System));
            Nav.NavigateTo($"/campaigns/{response.CampaignId}");
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _saving = false; }
    }

    private sealed class FormModel
    {
        [Required(ErrorMessage = "Введите название")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите систему")]
        [MaxLength(100)]
        public string System { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
