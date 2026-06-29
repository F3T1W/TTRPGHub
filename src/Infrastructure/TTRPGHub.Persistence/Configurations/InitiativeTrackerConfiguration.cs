using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class InitiativeTrackerConfiguration : IEntityTypeConfiguration<InitiativeTracker>
{
    public void Configure(EntityTypeBuilder<InitiativeTracker> builder)
    {
        builder.ToTable("initiative_trackers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new InitiativeTrackerId(v));

        builder.Property(t => t.CampaignId)
            .HasColumnName("campaign_id")
            .HasConversion(id => id.Value, v => new CampaignId(v));

        builder.Property(t => t.OwnerId)
            .HasColumnName("owner_id")
            .HasConversion(id => id.Value, v => new UserId(v));

        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Round).HasColumnName("round");
        builder.Property(t => t.ActiveEntryIndex).HasColumnName("active_entry_index");
        builder.Property(t => t.IsActive).HasColumnName("is_active");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsMany(t => t.Entries, e =>
        {
            e.ToTable("initiative_entries");
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            e.Property(x => x.Initiative).HasColumnName("initiative");
            e.Property(x => x.MaxHp).HasColumnName("max_hp");
            e.Property(x => x.CurrentHp).HasColumnName("current_hp");
            e.Property(x => x.ArmorClass).HasColumnName("armor_class");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
            e.Property(x => x.IsPlayerCharacter).HasColumnName("is_player_character");
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.WithOwner().HasForeignKey("tracker_id");
            e.HasKey("Id");
        });

        builder.HasIndex(t => t.CampaignId).HasDatabaseName("ix_initiative_trackers_campaign_id");
    }
}
