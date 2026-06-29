using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new CampaignId(v));

        builder.Property(c => c.OrganizerId)
            .HasColumnName("organizer_id")
            .HasConversion(id => id.Value, v => new UserId(v));

        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(c => c.System).HasColumnName("system").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsMany(c => c.Participants, p =>
        {
            p.ToTable("campaign_participants");
            p.Property(x => x.UserId)
                .HasColumnName("user_id")
                .HasConversion(id => id.Value, v => new UserId(v));
            p.Property(x => x.CampaignId)
                .HasColumnName("campaign_id")
                .HasConversion(id => id.Value, v => new CampaignId(v));
            p.Property(x => x.Role).HasColumnName("role").HasConversion<string>();
            p.Property(x => x.JoinedAt).HasColumnName("joined_at");
            p.HasKey("UserId", "CampaignId");
        });

        builder.HasIndex(c => c.OrganizerId).HasDatabaseName("ix_campaigns_organizer_id");
        builder.HasIndex(c => c.Status).HasDatabaseName("ix_campaigns_status");
    }
}
