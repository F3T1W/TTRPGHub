using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Configurations.Pf2e;

internal sealed class Pf2eMonsterConfiguration : IEntityTypeConfiguration<Pf2eMonster>
{
    public void Configure(EntityTypeBuilder<Pf2eMonster> builder)
    {
        builder.ToTable("pf2e_monsters");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new Pf2eMonsterId(v));

        builder.Property(m => m.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Size).HasColumnName("size").HasMaxLength(50);

        builder.Property(m => m.Traits).HasColumnName("traits");
        builder.Property(m => m.Senses).HasColumnName("senses");
        builder.Property(m => m.Languages).HasColumnName("languages");
        builder.Property(m => m.Skills).HasColumnName("skills");
        builder.Property(m => m.Speed).HasColumnName("speed");
        builder.Property(m => m.Attacks).HasColumnName("attacks");
        builder.Property(m => m.AttacksJson).HasColumnType("jsonb").HasColumnName("attacks_json");
        builder.Property(m => m.ResistancesJson).HasColumnType("jsonb").HasColumnName("resistances_json");
        builder.Property(m => m.WeaknessesJson).HasColumnType("jsonb").HasColumnName("weaknesses_json");
        builder.Property(m => m.ImmunitiesJson).HasColumnType("jsonb").HasColumnName("immunities_json");
        builder.Property(m => m.AurasJson).HasColumnType("jsonb").HasColumnName("auras_json");
        builder.Property(m => m.Abilities).HasColumnName("abilities");
        builder.Property(m => m.Source).HasColumnName("source");

        builder.Property(m => m.Level).HasColumnName("level");
        builder.Property(m => m.Perception).HasColumnName("perception");
        builder.Property(m => m.Strength).HasColumnName("strength");
        builder.Property(m => m.Dexterity).HasColumnName("dexterity");
        builder.Property(m => m.Constitution).HasColumnName("constitution");
        builder.Property(m => m.Intelligence).HasColumnName("intelligence");
        builder.Property(m => m.Wisdom).HasColumnName("wisdom");
        builder.Property(m => m.Charisma).HasColumnName("charisma");
        builder.Property(m => m.ArmorClass).HasColumnName("armor_class");
        builder.Property(m => m.Fortitude).HasColumnName("fortitude");
        builder.Property(m => m.Reflex).HasColumnName("reflex");
        builder.Property(m => m.Will).HasColumnName("will");
        builder.Property(m => m.HitPoints).HasColumnName("hit_points");

        builder.HasIndex(m => m.Slug).IsUnique().HasDatabaseName("ix_pf2e_monsters_slug");
        builder.HasIndex(m => m.Level).HasDatabaseName("ix_pf2e_monsters_level");
        builder.HasIndex(m => m.Size).HasDatabaseName("ix_pf2e_monsters_size");
    }
}
