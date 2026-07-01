using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class TableTokenConfiguration : IEntityTypeConfiguration<TableToken>
{
    public void Configure(EntityTypeBuilder<TableToken> builder)
    {
        builder.ToTable("table_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.SessionId)
            .HasConversion(id => id.Value, value => new GameSessionId(value))
            .HasColumnName("session_id")
            .IsRequired();

        builder.HasIndex(t => t.SessionId);

        builder.Property(t => t.Label).HasMaxLength(100).IsRequired().HasColumnName("label");
        builder.Property(t => t.ImageUrl).HasMaxLength(1000).HasColumnName("image_url");
        builder.Property(t => t.Color).HasMaxLength(20).IsRequired().HasColumnName("color");
        builder.Property(t => t.X).IsRequired().HasColumnName("x");
        builder.Property(t => t.Y).IsRequired().HasColumnName("y");

        builder.Property(t => t.OwnerId)
            .HasConversion(id => id!.Value.Value, value => new UserId(value))
            .HasColumnName("owner_id");

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).IsRequired().HasColumnName("updated_at");
    }
}
