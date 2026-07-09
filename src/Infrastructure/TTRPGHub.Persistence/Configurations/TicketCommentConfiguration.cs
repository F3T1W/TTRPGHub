using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("ticket_comments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new TicketCommentId(value))
            .HasColumnName("id");

        builder.Property(c => c.TicketId)
            .HasConversion(id => id.Value, value => new SupportTicketId(value))
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(c => c.AuthorId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("author_id")
            .IsRequired();

        builder.Property(c => c.Body).HasMaxLength(5000).IsRequired().HasColumnName("body");
        builder.Property(c => c.CreatedAt).IsRequired().HasColumnName("created_at");

        builder.HasIndex(c => c.TicketId);
    }
}
