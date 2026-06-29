using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Persistence.Configurations.Forum;

internal sealed class ForumPostConfiguration : IEntityTypeConfiguration<ForumPost>
{
    public void Configure(EntityTypeBuilder<ForumPost> builder)
    {
        builder.ToTable("forum_posts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => ForumPostId.From(v))
            .HasColumnName("id");

        builder.Property(x => x.TopicId)
            .HasConversion(id => id.Value, v => ForumTopicId.From(v))
            .HasColumnName("topic_id");

        builder.Property(x => x.AuthorId)
            .HasConversion(id => id.Value, v => new UserId(v))
            .HasColumnName("author_id");

        builder.Property(x => x.Content).HasColumnName("content").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Likes)
            .WithOne(x => x.Post)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
