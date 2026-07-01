using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class TableMessageConfiguration : IEntityTypeConfiguration<TableMessage>
{
    public void Configure(EntityTypeBuilder<TableMessage> builder)
    {
        builder.ToTable("table_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.SessionId)
            .HasConversion(id => id.Value, value => new GameSessionId(value))
            .HasColumnName("session_id")
            .IsRequired();

        builder.HasIndex(m => new { m.SessionId, m.CreatedAt });

        builder.Property(m => m.SenderId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("sender_id")
            .IsRequired();

        builder.Property(m => m.SenderUsername).HasMaxLength(100).IsRequired().HasColumnName("sender_username");

        builder.Property(m => m.RecipientId)
            .HasConversion(id => id!.Value.Value, value => new UserId(value))
            .HasColumnName("recipient_id");
        builder.Property(m => m.RecipientUsername).HasMaxLength(100).HasColumnName("recipient_username");

        builder.Property(m => m.Kind).HasConversion<string>().IsRequired().HasColumnName("kind");
        builder.Property(m => m.Content).HasMaxLength(2000).IsRequired().HasColumnName("content");
        builder.Property(m => m.CreatedAt).IsRequired().HasColumnName("created_at");
    }
}
