using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class CompanionConfiguration : IEntityTypeConfiguration<Companion>
{
    public void Configure(EntityTypeBuilder<Companion> builder)
    {
        builder.ToTable("companions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new CompanionId(v));

        builder.Property(c => c.OwnerCharacterId)
            .HasColumnName("owner_character_id")
            .HasConversion(id => id.Value, v => new CharacterId(v));

        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Kind).HasColumnName("kind").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Level).HasColumnName("level");
        builder.Property(c => c.MaxHitPoints).HasColumnName("max_hit_points");
        builder.Property(c => c.CurrentHitPoints).HasColumnName("current_hit_points");
        builder.Property(c => c.ArmorClass).HasColumnName("armor_class");
        builder.Property(c => c.Speed).HasColumnName("speed").HasMaxLength(50);
        builder.Property(c => c.AttacksText).HasColumnName("attacks_text");
        builder.Property(c => c.AbilitiesText).HasColumnName("abilities_text");
        builder.Property(c => c.Notes).HasColumnName("notes");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(c => c.OwnerCharacterId).HasDatabaseName("ix_companions_owner_character_id");
    }
}
