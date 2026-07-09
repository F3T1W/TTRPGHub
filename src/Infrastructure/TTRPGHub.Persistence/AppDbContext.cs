using MediatR;
using Microsoft.EntityFrameworkCore;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Entities.Events;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Entities.Moderation;

namespace TTRPGHub;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<Companion> Companions => Set<Companion>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SessionNote> SessionNotes => Set<SessionNote>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<InitiativeTracker> InitiativeTrackers => Set<InitiativeTracker>();
    public DbSet<Dnd5eSpell> Dnd5eSpells => Set<Dnd5eSpell>();
    public DbSet<Dnd5eMonster> Dnd5eMonsters => Set<Dnd5eMonster>();
    public DbSet<Pf2eSpell> Pf2eSpells => Set<Pf2eSpell>();
    public DbSet<Pf2eMonster> Pf2eMonsters => Set<Pf2eMonster>();
    public DbSet<Pf2eHazard> Pf2eHazards => Set<Pf2eHazard>();
    public DbSet<Pf2eVehicle> Pf2eVehicles => Set<Pf2eVehicle>();
    public DbSet<PathfinderSocietyChronicle> PathfinderSocietyChronicles => Set<PathfinderSocietyChronicle>();
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
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();
    public DbSet<DiscussionLike> DiscussionLikes => Set<DiscussionLike>();
    public DbSet<UserCalendarPreference> UserCalendarPreferences => Set<UserCalendarPreference>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<SessionReminderLog> SessionReminderLogs => Set<SessionReminderLog>();
    public DbSet<TableMessage> TableMessages => Set<TableMessage>();
    public DbSet<TableToken> TableTokens => Set<TableToken>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Macro> Macros => Set<Macro>();
    public DbSet<GameSystem> GameSystems => Set<GameSystem>();
    public DbSet<RuleEntry> RuleEntries => Set<RuleEntry>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<ModerationLogEntry> ModerationLogEntries => Set<ModerationLogEntry>();

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
