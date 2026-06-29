using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class PasswordResetToken : Entity<Guid>
{
    public UserId UserId { get; private init; }
    public string Token { get; private init; } = null!;
    public DateTime ExpiresAt { get; private init; }
    public bool IsUsed { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Create(UserId userId)
    {
        return new PasswordResetToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Token     = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            IsUsed    = false
        };
    }

    public bool IsValid() => !IsUsed && ExpiresAt > DateTime.UtcNow;

    public void MarkUsed() => IsUsed = true;
}
