using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class SessionReviewConfiguration : IEntityTypeConfiguration<SessionReview>
{
    public void Configure(EntityTypeBuilder<SessionReview> builder)
    {
        builder.ToTable("session_reviews");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => SessionReviewId.From(v));

        builder.Property(x => x.SessionId)
            .HasColumnName("session_id")
            .HasConversion(v => v.Value, v => new GameSessionId(v));

        builder.Property(x => x.ReviewerId)
            .HasColumnName("reviewer_id")
            .HasConversion(v => v.Value, v => new UserId(v));

        builder.Property(x => x.RevieweeId)
            .HasColumnName("reviewee_id")
            .HasConversion(v => v.Value, v => new UserId(v));

        builder.Property(x => x.Score).HasColumnName("score");
        builder.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Reviewee)
            .WithMany()
            .HasForeignKey(x => x.RevieweeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.ReviewerId, x.RevieweeId }).IsUnique();
        builder.HasIndex(x => x.RevieweeId);
    }
}
