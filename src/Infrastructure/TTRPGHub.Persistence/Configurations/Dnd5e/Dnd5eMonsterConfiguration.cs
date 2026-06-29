using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities.Dnd5e;

namespace TTRPGHub.Configurations.Dnd5e;

internal sealed class Dnd5eMonsterConfiguration : IEntityTypeConfiguration<Dnd5eMonster>
{
    public void Configure(EntityTypeBuilder<Dnd5eMonster> builder)
    {
        builder.ToTable("dnd5e_monsters");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new Dnd5eMonsterId(v));

        // HasMaxLength только на slug/name (уникальный индекс) и cr (индекс)
        builder.Property(m => m.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(m => m.ChallengeRating).HasColumnName("challenge_rating").HasMaxLength(10);

        // Всё остальное — text (без ограничения длины)
        builder.Property(m => m.Size).HasColumnName("size");
        builder.Property(m => m.Type).HasColumnName("type");
        builder.Property(m => m.Subtype).HasColumnName("subtype");
        builder.Property(m => m.Alignment).HasColumnName("alignment");
        builder.Property(m => m.ArmorDesc).HasColumnName("armor_desc");
        builder.Property(m => m.HitDice).HasColumnName("hit_dice");
        builder.Property(m => m.Speed).HasColumnName("speed");
        builder.Property(m => m.SenseStr).HasColumnName("senses");
        builder.Property(m => m.LanguagesStr).HasColumnName("languages");
        builder.Property(m => m.Source).HasColumnName("source");

        builder.Property(m => m.ArmorClass).HasColumnName("armor_class");
        builder.Property(m => m.HitPoints).HasColumnName("hit_points");
        builder.Property(m => m.Strength).HasColumnName("strength");
        builder.Property(m => m.Dexterity).HasColumnName("dexterity");
        builder.Property(m => m.Constitution).HasColumnName("constitution");
        builder.Property(m => m.Intelligence).HasColumnName("intelligence");
        builder.Property(m => m.Wisdom).HasColumnName("wisdom");
        builder.Property(m => m.Charisma).HasColumnName("charisma");
        builder.Property(m => m.Xp).HasColumnName("xp");
        builder.Property(m => m.Actions).HasColumnName("actions");
        builder.Property(m => m.SpecialAbilities).HasColumnName("special_abilities");
        builder.Property(m => m.Reactions).HasColumnName("reactions");
        builder.Property(m => m.LegendaryActions).HasColumnName("legendary_actions");

        builder.HasIndex(m => m.Slug).IsUnique().HasDatabaseName("ix_dnd5e_monsters_slug");
        builder.HasIndex(m => m.Type).HasDatabaseName("ix_dnd5e_monsters_type");
        builder.HasIndex(m => m.ChallengeRating).HasDatabaseName("ix_dnd5e_monsters_cr");
    }
}
