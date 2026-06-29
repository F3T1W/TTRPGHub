using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Persistence.Configurations.Forum;

internal sealed class ForumTopicConfiguration : IEntityTypeConfiguration<ForumTopic>
{
    public void Configure(EntityTypeBuilder<ForumTopic> builder)
    {
        builder.ToTable("forum_topics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => ForumTopicId.From(v))
            .HasColumnName("id");

        builder.Property(x => x.CategoryId)
            .HasConversion(id => id.Value, v => ForumCategoryId.From(v))
            .HasColumnName("category_id");

        builder.Property(x => x.AuthorId)
            .HasConversion(id => id.Value, v => new UserId(v))
            .HasColumnName("author_id");

        builder.Property(x => x.Title).HasMaxLength(200).HasColumnName("title").IsRequired();
        builder.Property(x => x.IsPinned).HasColumnName("is_pinned");
        builder.Property(x => x.IsLocked).HasColumnName("is_locked");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.LastPostAt).HasColumnName("last_post_at");

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Posts)
            .WithOne(x => x.Topic)
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
