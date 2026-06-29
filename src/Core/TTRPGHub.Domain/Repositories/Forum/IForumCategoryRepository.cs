using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Repositories.Forum;

public interface IForumCategoryRepository
{
    Task<List<ForumCategory>> GetAllAsync(CancellationToken ct = default);
    Task<ForumCategory?> GetByIdAsync(ForumCategoryId id, CancellationToken ct = default);
    Task<ForumCategory?> GetBySlugAsync(string slug, CancellationToken ct = default);
    void Add(ForumCategory category);
}
