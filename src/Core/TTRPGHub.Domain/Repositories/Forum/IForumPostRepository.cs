using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Repositories.Forum;

public interface IForumPostRepository
{
    Task<(List<ForumPost> Items, int Total)> GetByTopicAsync(
        ForumTopicId topicId, int page, int pageSize, UserId? currentUserId, CancellationToken ct = default);
    Task<ForumPost?> GetByIdAsync(ForumPostId id, CancellationToken ct = default);
    Task<bool> HasLikeAsync(ForumPostId postId, UserId userId, CancellationToken ct = default);
    void Add(ForumPost post);
    void AddLike(ForumPostLike like);
    void RemoveLike(ForumPostLike like);
}
