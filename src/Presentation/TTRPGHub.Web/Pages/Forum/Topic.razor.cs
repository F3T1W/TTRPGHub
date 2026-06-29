using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Forum;

public partial class Topic
{
    [Parameter] public Guid TopicId { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private ForumTopicDetailResult? _data;
    private bool _loading = true;
    private int _page = 1;
    private string _replyContent = "";
    private string? _replyError;

    protected override async Task OnParametersSetAsync()
    {
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
}
