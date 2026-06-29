using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Homebrew;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class HomebrewItemConfiguration : IEntityTypeConfiguration<HomebrewItem>
{
    public void Configure(EntityTypeBuilder<HomebrewItem> builder)
    {
        builder.ToTable("homebrew_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => HomebrewItemId.From(v))
            .HasColumnName("id");

        builder.Property(x => x.AuthorId)
            .HasConversion(id => id.Value, v => new UserId(v))
            .HasColumnName("author_id");

        builder.Property(x => x.Title).HasMaxLength(200).HasColumnName("title").IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).HasColumnName("description").IsRequired();
        builder.Property(x => x.System).HasMaxLength(100).HasColumnName("system").IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).HasColumnName("type");
        builder.Property(x => x.Content).HasColumnName("content").IsRequired();
        builder.Property(x => x.Tags).HasColumnName("tags");
        builder.Property(x => x.IsPublished).HasColumnName("is_published");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Likes).WithOne(x => x.Item).HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.System);
    }
}
