using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class SessionReminderLogConfiguration : IEntityTypeConfiguration<SessionReminderLog>
{
    public void Configure(EntityTypeBuilder<SessionReminderLog> builder)
    {
        builder.ToTable("session_reminder_logs");

        builder.HasKey(r => new { r.SessionId, r.UserId });

        builder.Property(r => r.SessionId)
            .HasConversion(id => id.Value, value => new GameSessionId(value))
            .HasColumnName("session_id");

        builder.Property(r => r.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("user_id");

        builder.Property(r => r.SentAt).IsRequired().HasColumnName("sent_at");
    }
}
