using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Persistence.Configurations.Forum;

internal sealed class ForumCategoryConfiguration : IEntityTypeConfiguration<ForumCategory>
{
    public void Configure(EntityTypeBuilder<ForumCategory> builder)
    {
        builder.ToTable("forum_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => ForumCategoryId.From(v))
            .HasColumnName("id");

        builder.Property(x => x.Name).HasMaxLength(100).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).HasColumnName("slug").IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.DisplayOrder).HasColumnName("display_order");

        builder.HasMany(x => x.Topics)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
