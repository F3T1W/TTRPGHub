using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class SceneConfiguration : IEntityTypeConfiguration<Scene>
{
    public void Configure(EntityTypeBuilder<Scene> builder)
    {
        builder.ToTable("scenes");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.SessionId)
            .HasConversion(id => id.Value, value => new GameSessionId(value))
            .HasColumnName("session_id")
            .IsRequired();
        builder.HasIndex(s => s.SessionId);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(s => s.SortOrder).IsRequired().HasDefaultValue(0).HasColumnName("sort_order");

        builder.Property(s => s.ShowcaseImageUrl).HasMaxLength(1000).HasColumnName("showcase_image_url");
        builder.Property(s => s.GridCellSizePx).IsRequired().HasDefaultValue(50).HasColumnName("grid_cell_size_px");
        builder.Property(s => s.FogEnabled).IsRequired().HasDefaultValue(false).HasColumnName("fog_enabled");
        builder.Property(s => s.VisionRadiusFeet).IsRequired().HasDefaultValue(30).HasColumnName("vision_radius_feet");
        builder.Property(s => s.WallsJson).HasColumnType("jsonb").HasColumnName("walls_json");
        builder.Property(s => s.LightsJson).HasColumnType("jsonb").HasColumnName("lights_json");
        builder.Property(s => s.TerrainTagsJson).HasColumnType("jsonb").HasColumnName("terrain_tags_json");
        builder.Property(s => s.AmbientLighting).HasMaxLength(32).IsRequired().HasDefaultValue("bright").HasColumnName("ambient_lighting");
        builder.Property(s => s.CombatActive).IsRequired().HasDefaultValue(false).HasColumnName("combat_active");
        builder.Property(s => s.CombatRound).IsRequired().HasDefaultValue(0).HasColumnName("combat_round");
        builder.Property(s => s.CombatTurnTokenId).HasColumnName("combat_turn_token_id");

        builder.Property(s => s.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).IsRequired().HasColumnName("updated_at");
    }
}
