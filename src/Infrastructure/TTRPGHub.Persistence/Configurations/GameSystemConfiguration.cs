using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class GameSystemConfiguration : IEntityTypeConfiguration<GameSystem>
{
    public void Configure(EntityTypeBuilder<GameSystem> builder)
    {
        builder.ToTable("game_systems");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new GameSystemId(value))
            .HasColumnName("id");

        builder.HasIndex(s => s.Slug).IsUnique();

        builder.Property(s => s.Slug).HasMaxLength(100).IsRequired().HasColumnName("slug");
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(s => s.IsOfficial).IsRequired().HasColumnName("is_official");

        builder.Property(s => s.CreatedByUserId)
            .HasConversion(id => id!.Value.Value, value => new UserId(value))
            .HasColumnName("created_by_user_id");

        builder.Property(s => s.CreatedAt).IsRequired().HasColumnName("created_at");
    }
}
