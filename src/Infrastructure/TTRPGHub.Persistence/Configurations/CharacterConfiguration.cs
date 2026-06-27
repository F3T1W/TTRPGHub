using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Domain.Entities;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("characters");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CharacterId(value))
            .HasColumnName("id");

        builder.Property(c => c.OwnerId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("owner_id")
            .IsRequired();

        builder.HasIndex(c => c.OwnerId);

        builder.Property(c => c.Name)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(c => c.Race)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("race");

        builder.Property(c => c.Class)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("class");

        builder.Property(c => c.Level)
            .IsRequired()
            .HasColumnName("level");

        builder.Property(c => c.Notes)
            .HasMaxLength(4096)
            .HasColumnName("notes");

        builder.Property(c => c.IsPublic)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_public");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");
    }
}
