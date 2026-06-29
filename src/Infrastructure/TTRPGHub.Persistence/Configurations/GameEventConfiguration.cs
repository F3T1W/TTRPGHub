using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class GameEventConfiguration : IEntityTypeConfiguration<GameEvent>
{
    public void Configure(EntityTypeBuilder<GameEvent> builder)
    {
        builder.ToTable("game_events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => GameEventId.From(v));

        builder.Property(x => x.OrganizerId)
            .HasColumnName("organizer_id")
            .HasConversion(v => v.Value, v => new UserId(v));

        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(3000);
        builder.Property(x => x.System).HasColumnName("system").HasMaxLength(100);
        builder.Property(x => x.Format).HasColumnName("format").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Location).HasColumnName("location").HasMaxLength(300);
        builder.Property(x => x.OnlineLink).HasColumnName("online_link").HasMaxLength(500);
        builder.Property(x => x.StartsAt).HasColumnName("starts_at");
        builder.Property(x => x.MaxParticipants).HasColumnName("max_participants");
        builder.Property(x => x.IsCancelled).HasColumnName("is_cancelled");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Organizer)
            .WithMany()
            .HasForeignKey(x => x.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Participants)
            .WithOne(x => x.Event)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.StartsAt);
        builder.HasIndex(x => x.OrganizerId);
    }
}
