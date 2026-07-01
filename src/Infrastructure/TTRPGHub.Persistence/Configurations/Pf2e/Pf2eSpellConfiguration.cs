using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Configurations.Pf2e;

internal sealed class Pf2eSpellConfiguration : IEntityTypeConfiguration<Pf2eSpell>
{
    public void Configure(EntityTypeBuilder<Pf2eSpell> builder)
    {
        builder.ToTable("pf2e_spells");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new Pf2eSpellId(v));

        builder.Property(s => s.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Level).HasColumnName("level");
        builder.Property(s => s.Traditions).HasColumnName("traditions").HasMaxLength(200);

        builder.Property(s => s.Traits).HasColumnName("traits");
        builder.Property(s => s.Cast).HasColumnName("cast");
        builder.Property(s => s.Range).HasColumnName("range");
        builder.Property(s => s.Area).HasColumnName("area");
        builder.Property(s => s.Targets).HasColumnName("targets");
        builder.Property(s => s.Duration).HasColumnName("duration");
        builder.Property(s => s.Description).HasColumnName("description");
        builder.Property(s => s.Heightened).HasColumnName("heightened");
        builder.Property(s => s.Source).HasColumnName("source");

        builder.HasIndex(s => s.Slug).IsUnique().HasDatabaseName("ix_pf2e_spells_slug");
        builder.HasIndex(s => s.Level).HasDatabaseName("ix_pf2e_spells_level");
        builder.HasIndex(s => s.Traditions).HasDatabaseName("ix_pf2e_spells_traditions");
    }
}
