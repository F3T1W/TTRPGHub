using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class RuleEntryConfiguration : IEntityTypeConfiguration<RuleEntry>
{
    public void Configure(EntityTypeBuilder<RuleEntry> builder)
    {
        builder.ToTable("rule_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new RuleEntryId(value))
            .HasColumnName("id");

        builder.Property(e => e.SystemId)
            .HasConversion(id => id.Value, value => new GameSystemId(value))
            .HasColumnName("system_id")
            .IsRequired();

        builder.Property(e => e.Category).HasConversion<string>().IsRequired().HasColumnName("category");
        builder.Property(e => e.Slug).HasMaxLength(150).IsRequired().HasColumnName("slug");

        builder.HasIndex(e => new { e.SystemId, e.Category, e.Slug }).IsUnique();
        builder.HasIndex(e => new { e.SystemId, e.Category });

        builder.Property(e => e.Title).HasMaxLength(300).IsRequired().HasColumnName("title");
        builder.Property(e => e.Summary).HasMaxLength(1000).HasColumnName("summary");
        builder.Property(e => e.ContentMarkdown).HasColumnName("content_markdown");

        builder.Property(e => e.StatsJson).HasColumnType("jsonb").IsRequired().HasColumnName("stats_json");

        builder.Property(e => e.Tags).HasColumnType("text[]").HasColumnName("tags");

        builder.Property(e => e.IsHomebrew).IsRequired().HasDefaultValue(false).HasColumnName("is_homebrew");
        builder.Property(e => e.Source).HasMaxLength(100).IsRequired().HasColumnName("source");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
    }
}
