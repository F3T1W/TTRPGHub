using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Discussions;

namespace TTRPGHub.Persistence.Configurations.Discussions;

internal sealed class DiscussionPostConfiguration : IEntityTypeConfiguration<DiscussionPost>
{
    public void Configure(EntityTypeBuilder<DiscussionPost> builder)
    {
        builder.ToTable("discussion_posts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => DiscussionPostId.From(v))
            .HasColumnName("id");

        builder.Property(x => x.EntityType)
            .HasConversion<string>()
            .HasColumnName("entity_type");

        builder.Property(x => x.EntitySlug)
            .HasColumnName("entity_slug")
            .IsRequired();

        builder.Property(x => x.AuthorId)
            .HasConversion(id => id.Value, v => new UserId(v))
            .HasColumnName("author_id");

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(x => x.ParentId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                v => v.HasValue ? DiscussionPostId.From(v.Value) : null)
            .HasColumnName("parent_id");

        builder.Property(x => x.LikeCount).HasColumnName("like_count");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => new { x.EntityType, x.EntitySlug });

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DiscussionPost>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Likes)
            .WithOne(x => x.Post)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
