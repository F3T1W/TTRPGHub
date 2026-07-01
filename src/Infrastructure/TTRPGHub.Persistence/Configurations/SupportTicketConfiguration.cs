using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new SupportTicketId(value))
            .HasColumnName("id");

        builder.Property(t => t.ReporterId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("reporter_id")
            .IsRequired();

        builder.HasIndex(t => t.ReporterId);
        builder.HasIndex(t => t.Status);

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired().HasColumnName("title");
        builder.Property(t => t.Description).HasMaxLength(5000).IsRequired().HasColumnName("description");
        builder.Property(t => t.ContactInfo).HasMaxLength(300).HasColumnName("contact_info");
        builder.Property(t => t.Status)
            .IsRequired()
            .HasDefaultValue(TicketStatus.Open)
            .HasConversion<string>()
            .HasColumnName("status");
        builder.Property(t => t.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).IsRequired().HasColumnName("updated_at");

        builder.OwnsMany(t => t.Attachments, a =>
        {
            a.ToTable("ticket_attachments");
            a.WithOwner().HasForeignKey("ticket_id");
            a.HasKey(x => x.Id);

            a.Property(x => x.Id).HasColumnName("id");
            a.Property(x => x.Url).HasMaxLength(1000).IsRequired().HasColumnName("url");
            a.Property(x => x.FileName).HasMaxLength(255).IsRequired().HasColumnName("file_name");
            a.Property(x => x.ContentType).HasMaxLength(100).IsRequired().HasColumnName("content_type");
        });
    }
}
