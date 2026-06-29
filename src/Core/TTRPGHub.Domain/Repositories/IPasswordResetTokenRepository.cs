using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    void Update(PasswordResetToken token);
}
