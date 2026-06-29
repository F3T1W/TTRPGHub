using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("game_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new GameSessionId(value))
            .HasColumnName("id");

        builder.Property(s => s.OrganizerId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("organizer_id")
            .IsRequired();

        builder.HasIndex(s => s.OrganizerId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.ScheduledAt);

        builder.Property(s => s.Title).HasMaxLength(200).IsRequired().HasColumnName("title");
        builder.Property(s => s.Description).HasMaxLength(2000).HasColumnName("description");
        builder.Property(s => s.System).HasMaxLength(100).IsRequired().HasColumnName("system");
        builder.Property(s => s.MaxPlayers).IsRequired().HasColumnName("max_players");
        builder.Property(s => s.ScheduledAt).IsRequired().HasColumnName("scheduled_at");
        builder.Property(s => s.Status)
            .IsRequired()
            .HasDefaultValue(SessionStatus.Planned)
            .HasColumnName("status")
            .HasConversion<string>();
        builder.Property(s => s.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).IsRequired().HasColumnName("updated_at");

        builder.OwnsMany(s => s.Participants, p =>
        {
            p.ToTable("session_participants");
            p.WithOwner().HasForeignKey("session_id");
            p.HasKey("SessionId", "UserId");

            p.Property(x => x.UserId)
                .HasConversion(id => id.Value, value => new UserId(value))
                .HasColumnName("user_id");
            p.Property(x => x.SessionId)
                .HasConversion(id => id.Value, value => new GameSessionId(value))
                .HasColumnName("session_id");
            p.Property(x => x.Role)
                .HasConversion<string>()
                .HasColumnName("role");
            p.Property(x => x.JoinedAt).HasColumnName("joined_at");
        });
    }
}
