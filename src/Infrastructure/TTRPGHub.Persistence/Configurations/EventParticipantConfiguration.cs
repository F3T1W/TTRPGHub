using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.ToTable("event_participants");

        builder.HasKey(x => new { x.EventId, x.UserId });

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .HasConversion(v => v.Value, v => GameEventId.From(v));

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion(v => v.Value, v => new UserId(v));

        builder.Property(x => x.RegisteredAt).HasColumnName("registered_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
