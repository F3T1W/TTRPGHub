using MediatR;
using Microsoft.EntityFrameworkCore;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Entities.Events;
using TTRPGHub.Entities.Ratings;

namespace TTRPGHub;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SessionNote> SessionNotes => Set<SessionNote>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<InitiativeTracker> InitiativeTrackers => Set<InitiativeTracker>();
    public DbSet<Dnd5eSpell> Dnd5eSpells => Set<Dnd5eSpell>();
    public DbSet<Dnd5eMonster> Dnd5eMonsters => Set<Dnd5eMonster>();
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ForumCategory> ForumCategories => Set<ForumCategory>();
    public DbSet<ForumTopic> ForumTopics => Set<ForumTopic>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumPostLike> ForumPostLikes => Set<ForumPostLike>();
    public DbSet<HomebrewItem> HomebrewItems => Set<HomebrewItem>();
    public DbSet<HomebrewLike> HomebrewLikes => Set<HomebrewLike>();
    public DbSet<UserRating> UserRatings => Set<UserRating>();
    public DbSet<GameEvent> GameEvents => Set<GameEvent>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var result = await base.SaveChangesAsync(ct);
        await DispatchDomainEventsAsync(ct);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var events = ChangeTracker.Entries<IHasDomainEvents>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<IHasDomainEvents>())
            entry.Entity.ClearDomainEvents();

        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, ct);
    }
}
