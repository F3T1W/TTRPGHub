using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IEmailConfirmationTokenRepository
{
    Task AddAsync(EmailConfirmationToken token, CancellationToken ct = default);
    Task<EmailConfirmationToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    void Update(EmailConfirmationToken token);
}
