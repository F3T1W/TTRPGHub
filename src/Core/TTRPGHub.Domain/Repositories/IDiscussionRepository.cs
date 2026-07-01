using TTRPGHub.Entities;
using TTRPGHub.Entities.Discussions;

namespace TTRPGHub.Repositories;

public interface IDiscussionRepository
{
    Task<IReadOnlyList<DiscussionPost>> GetByEntityAsync(DiscussionEntityType type, string slug, CancellationToken ct = default);
    Task<DiscussionPost?> GetByIdAsync(DiscussionPostId id, CancellationToken ct = default);
    Task<bool> HasLikedAsync(DiscussionPostId postId, UserId userId, CancellationToken ct = default);
    Task AddAsync(DiscussionPost post, CancellationToken ct = default);
    void Remove(DiscussionPost post);
    Task AddLikeAsync(DiscussionLike like, CancellationToken ct = default);
    void RemoveLike(DiscussionLike like);
    Task<DiscussionLike?> GetLikeAsync(DiscussionPostId postId, UserId userId, CancellationToken ct = default);
}
