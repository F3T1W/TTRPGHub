using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class SessionNoteConfiguration : IEntityTypeConfiguration<SessionNote>
{
    public void Configure(EntityTypeBuilder<SessionNote> builder)
    {
        builder.ToTable("session_notes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new SessionNoteId(v));

        builder.Property(n => n.CampaignId)
            .HasColumnName("campaign_id")
            .HasConversion(id => id.Value, v => new CampaignId(v));

        builder.Property(n => n.AuthorId)
            .HasColumnName("author_id")
            .HasConversion(id => id.Value, v => new UserId(v));

        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(n => n.Content).HasColumnName("content").HasMaxLength(50000).IsRequired();
        builder.Property(n => n.SessionDate).HasColumnName("session_date");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(n => n.CampaignId).HasDatabaseName("ix_session_notes_campaign_id");
        builder.HasIndex(n => n.AuthorId).HasDatabaseName("ix_session_notes_author_id");
    }
}
