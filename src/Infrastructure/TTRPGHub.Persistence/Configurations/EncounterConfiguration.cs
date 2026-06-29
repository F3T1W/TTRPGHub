using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.ToTable("encounters");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new EncounterId(v));

        builder.Property(e => e.CampaignId)
            .HasColumnName("campaign_id")
            .HasConversion(id => id.Value, v => new CampaignId(v));

        builder.Property(e => e.CreatedById)
            .HasColumnName("created_by_id")
            .HasConversion(id => id.Value, v => new UserId(v));

        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(e => e.Difficulty).HasColumnName("difficulty").HasConversion<string>().IsRequired();
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(5000);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsMany(e => e.Entries, entry =>
        {
            entry.ToTable("encounter_entries");
            entry.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entry.Property(x => x.Count).HasColumnName("count");
            entry.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
            entry.WithOwner().HasForeignKey("encounter_id");
            entry.HasKey("encounter_id", "Name");
        });

        builder.HasIndex(e => e.CampaignId).HasDatabaseName("ix_encounters_campaign_id");
    }
}
