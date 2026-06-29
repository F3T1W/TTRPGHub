using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Notes;

public partial class Create
{
    [Parameter] public Guid CampaignId { get; set; }
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
            var response = await Api.CreateNoteAsync(
                new CreateNoteRequest(CampaignId, _model.Title, _model.Content, _model.SessionDate));
            Nav.NavigateTo($"/notes/{response.NoteId}");
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _saving = false; }
    }

    private sealed class FormModel
    {
        [Required(ErrorMessage = "Введите заголовок")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите дату сессии")]
        public DateTime SessionDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Введите заметки")]
        [MaxLength(50000)]
        public string Content { get; set; } = string.Empty;
    }
}
