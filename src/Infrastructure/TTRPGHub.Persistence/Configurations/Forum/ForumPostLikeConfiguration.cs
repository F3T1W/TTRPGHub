using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Persistence.Configurations.Forum;

internal sealed class ForumPostLikeConfiguration : IEntityTypeConfiguration<ForumPostLike>
{
    public void Configure(EntityTypeBuilder<ForumPostLike> builder)
    {
        builder.ToTable("forum_post_likes");
        builder.HasKey(x => new { x.PostId, x.UserId });

        builder.Property(x => x.PostId)
            .HasConversion(id => id.Value, v => ForumPostId.From(v))
            .HasColumnName("post_id");

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, v => new UserId(v))
            .HasColumnName("user_id");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
