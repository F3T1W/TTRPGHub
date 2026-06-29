using TTRPGHub.Entities;
using TTRPGHub.Entities.Homebrew;

namespace TTRPGHub.Repositories;

public interface IHomebrewRepository
{
    Task<(List<HomebrewItem> Items, int Total)> SearchAsync(
        string? query, string? system, HomebrewType? type, string? tag,
        int page, int pageSize, CancellationToken ct = default);

    Task<HomebrewItem?> GetByIdAsync(HomebrewItemId id, CancellationToken ct = default);
    Task<List<HomebrewItem>> GetByAuthorAsync(UserId authorId, CancellationToken ct = default);
    Task<bool> HasLikeAsync(HomebrewItemId itemId, UserId userId, CancellationToken ct = default);
    void Add(HomebrewItem item);
    void Remove(HomebrewItem item);
    void AddLike(HomebrewLike like);
    void RemoveLike(HomebrewLike like);
}
