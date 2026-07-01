using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Discussions;

public partial class Thread
{
    [Parameter] public string EntityType { get; set; } = "";
    [Parameter] public string EntitySlug { get; set; } = "";
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    private List<DiscussionPostDto> _posts = [];
    private bool _loading = true;
    private string? _error;
    private string _newContent = "";
    private bool _submitting;
    private Guid? _replyingToId;
    private string _replyContent = "";
    private bool _isAuthenticated;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        _isAuthenticated = state.User.Identity?.IsAuthenticated ?? false;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _posts = await Api.GetDiscussionAsync(EntityType, EntitySlug);
        }
        catch (Exception ex)
        {
            _error = $"Не удалось загрузить обсуждение: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(_newContent)) return;
        _submitting = true;
        try
        {
            await Api.AddDiscussionPostAsync(EntityType, EntitySlug, new AddDiscussionPostRequest(_newContent));
            _newContent = "";
            await LoadAsync();
        }
        catch { }
        finally
        {
            _submitting = false;
        }
    }

    private async Task SubmitReplyAsync(Guid parentId)
    {
        if (string.IsNullOrWhiteSpace(_replyContent)) return;
        _submitting = true;
        try
        {
            await Api.AddDiscussionPostAsync(EntityType, EntitySlug, new AddDiscussionPostRequest(_replyContent, parentId));
            _replyContent = "";
            _replyingToId = null;
            await LoadAsync();
        }
        catch { }
        finally
        {
            _submitting = false;
        }
    }

    private async Task ToggleLikeAsync(Guid postId)
    {
        try
        {
            var response = await Api.ToggleDiscussionLikeAsync(postId);
            await LoadAsync();
        }
        catch { }
    }

    private async Task DeleteAsync(Guid postId)
    {
        try
        {
            await Api.DeleteDiscussionPostAsync(postId);
            await LoadAsync();
        }
        catch { }
    }

    private void StartReply(Guid postId)
    {
        if (_replyingToId == postId)
        {
            _replyingToId = null;
        }
        else
        {
            _replyingToId = postId;
            _replyContent = "";
        }
    }
}
