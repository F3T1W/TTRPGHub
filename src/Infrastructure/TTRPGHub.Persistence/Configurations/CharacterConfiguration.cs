using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

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

        builder.Property(c => c.CoOwnerIds)
            .HasColumnType("uuid[]")
            .HasColumnName("co_owner_ids")
            .HasDefaultValueSql("'{}'")
            .IsRequired();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired().HasColumnName("name");
        builder.Property(c => c.Race).HasMaxLength(50).IsRequired().HasColumnName("race");
        builder.Property(c => c.Class).HasMaxLength(50).IsRequired().HasColumnName("class");
        builder.Property(c => c.Level).IsRequired().HasColumnName("level");
        builder.Property(c => c.IsPublic).IsRequired().HasDefaultValue(false).HasColumnName("is_public");
        builder.Property(c => c.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).IsRequired().HasColumnName("updated_at");

        // Bio
        builder.Property(c => c.Background).HasMaxLength(100).HasColumnName("background");
        builder.Property(c => c.Alignment).HasMaxLength(50).HasColumnName("alignment");
        builder.Property(c => c.ExperiencePoints).HasDefaultValue(0).HasColumnName("experience_points");
        builder.Property(c => c.PersonalityTraits).HasMaxLength(1000).HasColumnName("personality_traits");
        builder.Property(c => c.Ideals).HasMaxLength(500).HasColumnName("ideals");
        builder.Property(c => c.Bonds).HasMaxLength(500).HasColumnName("bonds");
        builder.Property(c => c.Flaws).HasMaxLength(500).HasColumnName("flaws");

        // Stats
        builder.Property(c => c.Strength).HasDefaultValue(10).HasColumnName("strength");
        builder.Property(c => c.Dexterity).HasDefaultValue(10).HasColumnName("dexterity");
        builder.Property(c => c.Constitution).HasDefaultValue(10).HasColumnName("constitution");
        builder.Property(c => c.Intelligence).HasDefaultValue(10).HasColumnName("intelligence");
        builder.Property(c => c.Wisdom).HasDefaultValue(10).HasColumnName("wisdom");
        builder.Property(c => c.Charisma).HasDefaultValue(10).HasColumnName("charisma");

        // Combat stats
        builder.Property(c => c.MaxHitPoints).HasDefaultValue(0).HasColumnName("max_hit_points");
        builder.Property(c => c.CurrentHitPoints).HasDefaultValue(0).HasColumnName("current_hit_points");
        builder.Property(c => c.TemporaryHitPoints).HasDefaultValue(0).HasColumnName("temporary_hit_points");
        builder.Property(c => c.ArmorClass).HasDefaultValue(10).HasColumnName("armor_class");
        builder.Property(c => c.Speed).HasDefaultValue(30).HasColumnName("speed");
        builder.Property(c => c.HitDice).HasMaxLength(20).HasDefaultValue("1d8").HasColumnName("hit_dice");

        // Skills
        builder.Property(c => c.SkillProficiencies)
            .HasColumnType("text[]")
            .HasColumnName("skill_proficiencies");

        builder.Property(c => c.SavingThrowProficiencies)
            .HasColumnType("text[]")
            .HasColumnName("saving_throw_proficiencies");

        // Trais and equip
        builder.Property(c => c.FeaturesAndTraits).HasColumnName("features_and_traits");
        builder.Property(c => c.Equipment).HasColumnName("equipment");

        // Profile picture
        builder.Property(c => c.AvatarUrl).HasMaxLength(500).HasColumnName("avatar_url");

        // PF2e-лист (см. комментарий на Character.Pf2eStatsJson)
        builder.Property(c => c.Pf2eStatsJson).HasColumnType("jsonb").HasColumnName("pf2e_stats_json");
        builder.Property(c => c.SelectedFeatsJson).HasColumnType("jsonb").HasColumnName("selected_feats_json");

        // Calculated props
        builder.Ignore(c => c.ProficiencyBonus);
        builder.Ignore(c => c.StrengthModifier);
        builder.Ignore(c => c.DexterityModifier);
        builder.Ignore(c => c.ConstitutionModifier);
        builder.Ignore(c => c.IntelligenceModifier);
        builder.Ignore(c => c.WisdomModifier);
        builder.Ignore(c => c.CharismaModifier);
        builder.Ignore(c => c.Initiative);
    }
}
