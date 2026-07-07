using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.SessionId)
            .HasConversion(id => id.Value, value => new GameSessionId(value))
            .HasColumnName("session_id")
            .IsRequired();

        builder.HasIndex(e => new { e.SessionId, e.CreatedAt });

        builder.Property(e => e.AuthorId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("author_id")
            .IsRequired();

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired().HasColumnName("title");
        builder.Property(e => e.ContentMarkdown).IsRequired().HasColumnName("content_markdown");
        builder.Property(e => e.IsPublished).IsRequired().HasDefaultValue(false).HasColumnName("is_published");
        builder.Property(e => e.ParentId).HasColumnName("parent_id");
        builder.Property(e => e.VisibleToUserIdsJson).HasColumnType("jsonb").HasColumnName("visible_to_user_ids_json");
        builder.Property(e => e.CampaignId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new CampaignId(value.Value) : null)
            .HasColumnName("campaign_id");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
    }
}
