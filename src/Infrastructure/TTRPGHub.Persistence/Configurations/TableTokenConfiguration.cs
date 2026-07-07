using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class TableTokenConfiguration : IEntityTypeConfiguration<TableToken>
{
    public void Configure(EntityTypeBuilder<TableToken> builder)
    {
        builder.ToTable("table_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.SessionId)
            .HasConversion(id => id.Value, value => new GameSessionId(value))
            .HasColumnName("session_id")
            .IsRequired();

        builder.HasIndex(t => t.SessionId);

        builder.Property(t => t.SceneId).IsRequired().HasColumnName("scene_id");
        builder.HasIndex(t => t.SceneId);

        builder.Property(t => t.Label).HasMaxLength(100).IsRequired().HasColumnName("label");
        builder.Property(t => t.ImageUrl).HasMaxLength(1000).HasColumnName("image_url");
        builder.Property(t => t.Color).HasMaxLength(20).IsRequired().HasColumnName("color");
        builder.Property(t => t.X).IsRequired().HasColumnName("x");
        builder.Property(t => t.Y).IsRequired().HasColumnName("y");
        builder.Property(t => t.Width).IsRequired().HasDefaultValue(1).HasColumnName("width");
        builder.Property(t => t.Height).IsRequired().HasDefaultValue(1).HasColumnName("height");
        builder.Property(t => t.Rotation).IsRequired().HasDefaultValue(0).HasColumnName("rotation");
        builder.Property(t => t.Initiative).HasColumnName("initiative");
        builder.Property(t => t.HasDarkvision).IsRequired().HasDefaultValue(false).HasColumnName("has_darkvision");
        builder.Property(t => t.HasLowLightVision).IsRequired().HasDefaultValue(false).HasColumnName("has_low_light_vision");
        builder.Property(t => t.VisibleToJson).HasColumnType("jsonb").HasColumnName("visible_to_json");

        builder.Property(t => t.OwnerId)
            .HasConversion(id => id!.Value.Value, value => new UserId(value))
            .HasColumnName("owner_id");

        builder.Property(t => t.CombatantType)
            .IsRequired()
            .HasDefaultValue(TokenCombatantType.None)
            .HasConversion<string>()
            .HasColumnName("combatant_type");
        builder.Property(t => t.CombatantId).HasColumnName("combatant_id");
        builder.Property(t => t.CurrentHp).HasColumnName("current_hp");
        builder.Property(t => t.MaxHp).HasColumnName("max_hp");
        builder.Property(t => t.ArmorClass).HasColumnName("armor_class");

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).IsRequired().HasColumnName("updated_at");

        builder.OwnsMany(t => t.Conditions, c =>
        {
            c.ToTable("token_conditions");
            c.WithOwner().HasForeignKey("token_id");
            c.HasKey(x => x.Id);

            // ValueGeneratedNever: без этого EF считает непустой клиентский Guid признаком
            // "уже существующей" строки (эвристика "ключ != default => Modified, не Added")
            // и пытается выполнить UPDATE для только что добавленного в коллекцию условия
            // вместо INSERT — DbUpdateConcurrencyException (0 строк вместо 1). С этой настройкой
            // EF полагается на сравнение снапшота коллекции, а не на значение ключа.
            c.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            c.Property(x => x.Slug).HasMaxLength(100).IsRequired().HasColumnName("slug");
            c.Property(x => x.Name).HasMaxLength(100).IsRequired().HasColumnName("name");
            c.Property(x => x.Value).HasColumnName("value");
            c.Property(x => x.AppliedAt).IsRequired().HasColumnName("applied_at");
        });
    }
}
