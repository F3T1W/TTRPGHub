using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
    Task<(IReadOnlyList<User> Items, int Total)> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
