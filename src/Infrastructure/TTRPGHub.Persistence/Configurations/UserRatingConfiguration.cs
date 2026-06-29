using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class UserRatingConfiguration : IEntityTypeConfiguration<UserRating>
{
    public void Configure(EntityTypeBuilder<UserRating> builder)
    {
        builder.ToTable("user_ratings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => UserRatingId.From(v));

        builder.Property(x => x.RaterId)
            .HasColumnName("rater_id")
            .HasConversion(v => v.Value, v => new UserId(v));

        builder.Property(x => x.RateeId)
            .HasColumnName("ratee_id")
            .HasConversion(v => v.Value, v => new UserId(v));

        builder.Property(x => x.Score).HasColumnName("score");
        builder.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Rater)
            .WithMany()
            .HasForeignKey(x => x.RaterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Ratee)
            .WithMany()
            .HasForeignKey(x => x.RateeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RaterId, x.RateeId }).IsUnique();
        builder.HasIndex(x => x.RateeId);
    }
}
