using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class PasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default) =>
        await db.PasswordResetTokens.AddAsync(token, ct);

    public Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public void Update(PasswordResetToken token) =>
        db.PasswordResetTokens.Update(token);
}
