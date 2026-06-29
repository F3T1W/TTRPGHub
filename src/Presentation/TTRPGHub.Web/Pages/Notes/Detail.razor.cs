using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Notes;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private SessionNoteDetailDto? _note;
    private bool _loading = true;
    private bool _editing;
    private bool _saving;
    private string? _error;
    private readonly EditFormModel _editModel = new();

    protected override async Task OnInitializedAsync() => await Load();

    private async Task Load()
    {
        _loading = true;
        try { _note = await Api.GetNoteDetailAsync(Id); }
        catch { _note = null; }
        finally { _loading = false; }
    }

    private void StartEdit()
    {
        _editModel.Title       = _note!.Title;
        _editModel.Content     = _note.Content;
        _editModel.SessionDate = _note.SessionDate;
        _editing = true;
    }

    private void CancelEdit() => _editing = false;

    private async Task SaveEdit()
    {
        _saving = true;
        _error = null;
        try
        {
            await Api.UpdateNoteAsync(Id, new UpdateNoteRequest(_editModel.Title, _editModel.Content, _editModel.SessionDate));
            _editing = false;
            await Load();
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _saving = false; }
    }

    private async Task DeleteNote()
    {
        try
        {
            await Api.DeleteNoteAsync(Id);
            Nav.NavigateTo($"/campaigns/{_note!.CampaignId}");
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private sealed class EditFormModel
    {
        [Required(ErrorMessage = "Введите заголовок")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите дату сессии")]
        public DateTime SessionDate { get; set; }

        [Required(ErrorMessage = "Введите заметки")]
        [MaxLength(50000)]
        public string Content { get; set; } = string.Empty;
    }
}
