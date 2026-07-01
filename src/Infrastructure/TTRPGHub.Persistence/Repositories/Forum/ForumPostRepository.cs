using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Persistence.Repositories.Forum;

internal sealed class ForumPostRepository(AppDbContext db) : IForumPostRepository
{
    public async Task<(List<ForumPost> Items, int Total)> GetByTopicAsync(
        ForumTopicId topicId, int page, int pageSize, UserId? currentUserId, CancellationToken ct)
    {
        var query = db.ForumPosts
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Where(p => p.TopicId == topicId)
            .OrderBy(p => p.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<ForumPost?> GetByIdAsync(ForumPostId id, CancellationToken ct) =>
        db.ForumPosts
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> HasLikeAsync(ForumPostId postId, UserId userId, CancellationToken ct) =>
        db.ForumPostLikes.AnyAsync(l => l.PostId == postId && l.UserId == userId, ct);

    public void Add(ForumPost post) => db.ForumPosts.Add(post);
    public void Remove(ForumPost post) => db.ForumPosts.Remove(post);
    public void AddLike(ForumPostLike like) => db.ForumPostLikes.Add(like);
    public void RemoveLike(ForumPostLike like) => db.ForumPostLikes.Remove(like);
}
