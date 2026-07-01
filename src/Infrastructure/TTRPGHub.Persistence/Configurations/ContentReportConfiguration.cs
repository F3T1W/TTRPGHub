using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Moderation;

namespace TTRPGHub.Configurations;

internal sealed class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport>
{
    public void Configure(EntityTypeBuilder<ContentReport> builder)
    {
        builder.ToTable("content_reports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new ContentReportId(value))
            .HasColumnName("id");

        builder.Property(r => r.ReporterId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("reporter_id")
            .IsRequired();

        builder.Property(r => r.ResolvedByUserId)
            .HasConversion(id => id!.Value.Value, value => new UserId(value))
            .HasColumnName("resolved_by_user_id");

        builder.Property(r => r.EntityType).HasConversion<string>().IsRequired().HasColumnName("entity_type");
        builder.Property(r => r.EntityId).IsRequired().HasColumnName("entity_id");
        builder.Property(r => r.Reason).HasMaxLength(1000).IsRequired().HasColumnName("reason");
        builder.Property(r => r.Status)
            .IsRequired()
            .HasDefaultValue(ReportStatus.Open)
            .HasConversion<string>()
            .HasColumnName("status");
        builder.Property(r => r.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(r => r.ResolvedAt).HasColumnName("resolved_at");

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.EntityType, r.EntityId });
    }
}
