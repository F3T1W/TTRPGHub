using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Admin;

public partial class Users
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private AdminUserPageDto? _result;
    private bool _loading = true;
    private string? _error;
    private string? _saveError;
    private string _search = string.Empty;
    private int _page = 1;
    private const int PageSize = 30;
    private Guid? _savingId;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _result = await Api.GetAllUsersAsync(string.IsNullOrWhiteSpace(_search) ? null : _search, _page, PageSize);
        }
        catch
        {
            _error = "Не удалось загрузить список пользователей.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") { _page = 1; await LoadAsync(); }
    }

    private async Task PrevPage() { if (_page > 1) { _page--; await LoadAsync(); } }
    private async Task NextPage() { if (_result is not null && _page < _result.TotalPages) { _page++; await LoadAsync(); } }

    private async Task ChangeRoleAsync(Guid userId, string newRole)
    {
        _savingId = userId;
        _saveError = null;
        try
        {
            await Api.ChangeUserRoleAsync(userId, new ChangeRoleRequest(newRole));
            await LoadAsync();
        }
        catch
        {
            _saveError = "Не удалось изменить роль (нельзя менять собственную роль).";
        }
        finally
        {
            _savingId = null;
        }
    }
}
