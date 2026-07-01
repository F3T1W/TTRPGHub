using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Endpoint).IsUnique();

        builder.Property(p => p.Endpoint).HasMaxLength(1000).IsRequired().HasColumnName("endpoint");
        builder.Property(p => p.P256dh).HasMaxLength(500).IsRequired().HasColumnName("p256dh");
        builder.Property(p => p.Auth).HasMaxLength(500).IsRequired().HasColumnName("auth");
        builder.Property(p => p.CreatedAt).IsRequired().HasColumnName("created_at");
    }
}
