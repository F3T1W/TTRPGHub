using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Configurations.Pf2e;

internal sealed class PathfinderSocietyChronicleConfiguration : IEntityTypeConfiguration<PathfinderSocietyChronicle>
{
    public void Configure(EntityTypeBuilder<PathfinderSocietyChronicle> builder)
    {
        builder.ToTable("pathfinder_society_chronicles");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new PathfinderSocietyChronicleId(v));

        builder.Property(c => c.CharacterId)
            .HasColumnName("character_id")
            .HasConversion(id => id.Value, v => new CharacterId(v));

        builder.Property(c => c.ScenarioName).HasColumnName("scenario_name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.SessionDate).HasColumnName("session_date");
        builder.Property(c => c.GmName).HasColumnName("gm_name").HasMaxLength(100);
        builder.Property(c => c.Faction).HasColumnName("faction").HasMaxLength(100);
        builder.Property(c => c.GoldEarned).HasColumnName("gold_earned");
        builder.Property(c => c.AchievementPoints).HasColumnName("achievement_points");
        builder.Property(c => c.BoonsUsed).HasColumnName("boons_used");
        builder.Property(c => c.Notes).HasColumnName("notes");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(c => c.CharacterId).HasDatabaseName("ix_pathfinder_society_chronicles_character_id");
    }
}
