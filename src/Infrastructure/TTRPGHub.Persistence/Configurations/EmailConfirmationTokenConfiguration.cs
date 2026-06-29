using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class EmailConfirmationTokenConfiguration : IEntityTypeConfiguration<EmailConfirmationToken>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationToken> builder)
    {
        builder.ToTable("email_confirmation_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, v => new UserId(v));
        builder.Property(t => t.Token).HasColumnName("token").HasMaxLength(64).IsRequired();
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.IsUsed).HasColumnName("is_used");

        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("ix_email_confirmation_tokens_token");
    }
}
