using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Events;
using TTRPGHub.Domain.ValueObjects;

namespace TTRPGHub.Domain.Entities;

public sealed class User : Entity<UserId>
{
    public string Username { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private init; }
    public UserProfile Profile { get; private set; } = null!;

    private User() { }

    public static User Create(string username, Email email, string passwordHash)
    {
        var user = new User
        {
            Id = UserId.New(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            Profile = UserProfile.Default()
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id));
        return user;
    }

    public void UpdateProfile(string? displayName, string? bio, string? city) =>
        Profile = Profile.Update(displayName, bio, city);
}

public sealed record UserProfile(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    string? City,
    ExperienceLevel ExperienceLevel)
{
    public static UserProfile Default() => new(null, null, null, null, ExperienceLevel.Beginner);

    public UserProfile Update(string? displayName, string? bio, string? city) =>
        this with { DisplayName = displayName, Bio = bio, City = city };
}

public enum ExperienceLevel { Beginner, Intermediate, Experienced, Veteran }
