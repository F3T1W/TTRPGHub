using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class MacroConfiguration : IEntityTypeConfiguration<Macro>
{
    public void Configure(EntityTypeBuilder<Macro> builder)
    {
        builder.ToTable("macros");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.OwnerId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("owner_id")
            .IsRequired();

        builder.HasIndex(m => m.OwnerId);

        builder.Property(m => m.Name).HasMaxLength(100).IsRequired().HasColumnName("name");
        builder.Property(m => m.ImageUrl).HasMaxLength(500).HasColumnName("image_url");
        builder.Property(m => m.Type).IsRequired().HasConversion<string>().HasColumnName("type");
        builder.Property(m => m.Command).IsRequired().HasColumnName("command");
        builder.Property(m => m.HotbarSlot).IsRequired().HasDefaultValue(-1).HasColumnName("hotbar_slot");
        builder.Property(m => m.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).IsRequired().HasColumnName("updated_at");
    }
}
