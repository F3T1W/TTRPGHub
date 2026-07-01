using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Discussions;

namespace TTRPGHub.Persistence.Configurations.Discussions;

internal sealed class DiscussionLikeConfiguration : IEntityTypeConfiguration<DiscussionLike>
{
    public void Configure(EntityTypeBuilder<DiscussionLike> builder)
    {
        builder.ToTable("discussion_likes");
        builder.HasKey(x => new { x.PostId, x.UserId });

        builder.Property(x => x.PostId)
            .HasConversion(id => id.Value, v => DiscussionPostId.From(v))
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
