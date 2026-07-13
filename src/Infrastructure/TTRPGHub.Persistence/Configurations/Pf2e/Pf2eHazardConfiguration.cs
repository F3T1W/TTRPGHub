using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Configurations.Pf2e;

internal sealed class Pf2eHazardConfiguration : IEntityTypeConfiguration<Pf2eHazard>
{
    public void Configure(EntityTypeBuilder<Pf2eHazard> builder)
    {
        builder.ToTable("pf2e_hazards");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new Pf2eHazardId(v));

        builder.Property(h => h.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(h => h.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(h => h.NameRu).HasColumnName("name_ru").HasMaxLength(200).IsRequired();
        builder.Property(h => h.Traits).HasColumnName("traits");
        builder.Property(h => h.StealthNote).HasColumnName("stealth_note").HasMaxLength(100);
        builder.Property(h => h.Description).HasColumnName("description");
        builder.Property(h => h.DisableText).HasColumnName("disable_text");
        builder.Property(h => h.Immunities).HasColumnName("immunities");
        builder.Property(h => h.AbilitiesText).HasColumnName("abilities_text");
        builder.Property(h => h.ResetText).HasColumnName("reset_text");
        builder.Property(h => h.Source).HasColumnName("source").HasMaxLength(300);
        builder.Property(h => h.AttacksJson).HasColumnType("jsonb").HasColumnName("attacks_json");

        builder.Property(h => h.Level).HasColumnName("level");
        builder.Property(h => h.StealthDc).HasColumnName("stealth_dc");
        builder.Property(h => h.ArmorClass).HasColumnName("armor_class");
        builder.Property(h => h.Fortitude).HasColumnName("fortitude");
        builder.Property(h => h.Reflex).HasColumnName("reflex");
        builder.Property(h => h.Hardness).HasColumnName("hardness");
        builder.Property(h => h.HitPoints).HasColumnName("hit_points");

        builder.HasIndex(h => h.Slug).IsUnique().HasDatabaseName("ix_pf2e_hazards_slug");
        builder.HasIndex(h => h.Level).HasDatabaseName("ix_pf2e_hazards_level");
    }
}
