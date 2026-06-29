using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class EmailConfirmationTokenRepository(AppDbContext db) : IEmailConfirmationTokenRepository
{
    public async Task AddAsync(EmailConfirmationToken token, CancellationToken ct = default) =>
        await db.EmailConfirmationTokens.AddAsync(token, ct);

    public Task<EmailConfirmationToken?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        db.EmailConfirmationTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public void Update(EmailConfirmationToken token) =>
        db.EmailConfirmationTokens.Update(token);
}
