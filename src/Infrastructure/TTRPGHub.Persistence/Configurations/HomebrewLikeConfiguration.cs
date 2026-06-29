using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Homebrew;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class HomebrewLikeConfiguration : IEntityTypeConfiguration<HomebrewLike>
{
    public void Configure(EntityTypeBuilder<HomebrewLike> builder)
    {
        builder.ToTable("homebrew_likes");
        builder.HasKey(x => new { x.ItemId, x.UserId });

        builder.Property(x => x.ItemId)
            .HasConversion(id => id.Value, v => HomebrewItemId.From(v))
            .HasColumnName("item_id");

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, v => new UserId(v))
            .HasColumnName("user_id");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
