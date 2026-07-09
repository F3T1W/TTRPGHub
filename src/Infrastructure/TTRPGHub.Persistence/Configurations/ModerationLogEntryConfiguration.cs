using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Moderation;

namespace TTRPGHub.Configurations;

internal sealed class ModerationLogEntryConfiguration : IEntityTypeConfiguration<ModerationLogEntry>
{
    public void Configure(EntityTypeBuilder<ModerationLogEntry> builder)
    {
        builder.ToTable("moderation_log_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ModerationLogEntryId(value))
            .HasColumnName("id");

        builder.Property(e => e.ActorUserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("actor_user_id")
            .IsRequired();

        builder.Property(e => e.Action).HasMaxLength(100).IsRequired().HasColumnName("action");
        builder.Property(e => e.TargetType).HasMaxLength(100).IsRequired().HasColumnName("target_type");
        builder.Property(e => e.TargetId).IsRequired().HasColumnName("target_id");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(e => e.Details).HasMaxLength(2000).HasColumnName("details");

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.ActorUserId);
    }
}
