using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities.Dnd5e;

namespace TTRPGHub.Configurations.Dnd5e;

internal sealed class Dnd5eSpellConfiguration : IEntityTypeConfiguration<Dnd5eSpell>
{
    public void Configure(EntityTypeBuilder<Dnd5eSpell> builder)
    {
        builder.ToTable("dnd5e_spells");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new Dnd5eSpellId(v));

        // HasMaxLength только на индексируемых полях
        builder.Property(s => s.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Level).HasColumnName("level");
        builder.Property(s => s.School).HasColumnName("school").HasMaxLength(50);

        // text — данные из Open5e непредсказуемо длинные
        builder.Property(s => s.CastingTime).HasColumnName("casting_time");
        builder.Property(s => s.Range).HasColumnName("range");
        builder.Property(s => s.Components).HasColumnName("components");
        builder.Property(s => s.Material).HasColumnName("material");
        builder.Property(s => s.Duration).HasColumnName("duration");
        builder.Property(s => s.Classes).HasColumnName("classes");
        builder.Property(s => s.Source).HasColumnName("source");

        builder.Property(s => s.Concentration).HasColumnName("concentration");
        builder.Property(s => s.Ritual).HasColumnName("ritual");
        builder.Property(s => s.Description).HasColumnName("description");
        builder.Property(s => s.HigherLevel).HasColumnName("higher_level");

        builder.HasIndex(s => s.Slug).IsUnique().HasDatabaseName("ix_dnd5e_spells_slug");
        builder.HasIndex(s => s.Level).HasDatabaseName("ix_dnd5e_spells_level");
        builder.HasIndex(s => s.School).HasDatabaseName("ix_dnd5e_spells_school");
    }
}
