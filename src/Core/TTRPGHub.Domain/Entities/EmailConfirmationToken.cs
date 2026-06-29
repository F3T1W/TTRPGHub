using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class EmailConfirmationToken : Entity<Guid>
{
    public UserId UserId { get; private init; }
    public string Token { get; private init; } = null!;
    public DateTime ExpiresAt { get; private init; }
    public bool IsUsed { get; private set; }

    private EmailConfirmationToken() { }

    public static EmailConfirmationToken Create(UserId userId)
    {
        return new EmailConfirmationToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Token     = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed    = false
        };
    }

    public bool IsValid() => !IsUsed && ExpiresAt > DateTime.UtcNow;

    public void MarkUsed() => IsUsed = true;
}
