using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Forum;

public partial class Topic
{
    [Parameter] public Guid TopicId { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private TokenStorage TokenStorage { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private ForumTopicDetailResult? _data;
    private bool _loading = true;
    private int _page = 1;
    private string _replyContent = "";
    private string? _replyError;
    private Guid? _currentUserId;
    private string? _actionMessage;

    protected override async Task OnParametersSetAsync()
    {
        _currentUserId = await TokenStorage.GetUserIdAsync();
        await LoadPageAsync(1);
    }

    private async Task LoadPageAsync(int page)
    {
        _loading = true;
        _page = page;
        _data = await Api.GetForumPostsAsync(TopicId, page, 20);
        _loading = false;
    }

    private async Task ReplyAsync()
    {
        _replyError = null;
        if (string.IsNullOrWhiteSpace(_replyContent))
        {
            _replyError = "Введи текст сообщения";
            return;
        }

        try
        {
            await Api.CreateForumPostAsync(TopicId, new CreateForumPostRequest(_replyContent));
            _replyContent = "";
            await LoadPageAsync(_data!.Posts.TotalPages);
        }
        catch
        {
            _replyError = "Не удалось отправить сообщение";
        }
    }

    private async Task ToggleLikeAsync(ForumPostDto post)
    {
        try
        {
            var result = await Api.ToggleForumPostLikeAsync(post.Id);
            // Обновляем локально без перезагрузки
            var idx = _data!.Posts.Items.IndexOf(post);
            if (idx >= 0)
                _data.Posts.Items[idx] = post with { LikedByMe = result.Liked, LikeCount = result.LikeCount };
        }
        catch { }
    }

    private async Task ReportPostAsync(Guid postId)
    {
        try
        {
            await Api.CreateReportAsync(new CreateReportRequest("ForumPost", postId, "Жалоба на сообщение форума"));
            _actionMessage = "Жалоба отправлена, модераторы её рассмотрят.";
        }
        catch
        {
            _actionMessage = "Не удалось отправить жалобу.";
        }
    }

    private async Task DeletePostAsync(Guid postId)
    {
        try
        {
            await Api.DeleteForumPostAsync(postId);
            await LoadPageAsync(_page);
        }
        catch
        {
            _actionMessage = "Не удалось удалить сообщение.";
        }
    }

    private async Task DeleteTopicAsync()
    {
        try
        {
            await Api.DeleteForumTopicAsync(TopicId);
            Nav.NavigateTo($"/forum/{_data?.CategorySlug}");
        }
        catch
        {
            _actionMessage = "Не удалось удалить тему.";
        }
    }

    private async Task TogglePinAsync()
    {
        try
        {
            await Api.SetForumTopicPinnedAsync(TopicId, new SetPinnedRequest(!_data!.IsPinned));
            await LoadPageAsync(_page);
        }
        catch
        {
            _actionMessage = "Не удалось изменить закрепление темы.";
        }
    }

    private async Task ToggleLockAsync()
    {
        try
        {
            await Api.SetForumTopicLockedAsync(TopicId, new SetLockedRequest(!_data!.IsLocked));
            await LoadPageAsync(_page);
        }
        catch
        {
            _actionMessage = "Не удалось изменить блокировку темы.";
        }
    }
}
