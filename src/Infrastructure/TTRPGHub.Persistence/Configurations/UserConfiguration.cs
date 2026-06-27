using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Domain.Entities;

namespace TTRPGHub.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("id");

        builder.Property(u => u.Username)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("username");

        builder.HasIndex(u => u.Username).IsUnique();

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasMaxLength(256)
                .IsRequired()
                .HasColumnName("email");
            email.HasIndex(e => e.Value).IsUnique();
        });

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(128)
            .IsRequired()
            .HasColumnName("password_hash");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.OwnsOne(u => u.Profile, profile =>
        {
            profile.Property(p => p.DisplayName).HasMaxLength(64).HasColumnName("display_name");
            profile.Property(p => p.AvatarUrl).HasMaxLength(512).HasColumnName("avatar_url");
            profile.Property(p => p.Bio).HasMaxLength(500).HasColumnName("bio");
            profile.Property(p => p.City).HasMaxLength(64).HasColumnName("city");
            profile.Property(p => p.ExperienceLevel)
                .HasConversion<string>()
                .HasColumnName("experience_level");
        });
    }
}
