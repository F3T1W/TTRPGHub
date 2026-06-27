using MediatR;
using Microsoft.EntityFrameworkCore;
using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Entities;

namespace TTRPGHub.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();

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
