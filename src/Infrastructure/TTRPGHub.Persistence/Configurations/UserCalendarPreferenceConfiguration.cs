using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities;

namespace TTRPGHub.Configurations;

internal sealed class UserCalendarPreferenceConfiguration : IEntityTypeConfiguration<UserCalendarPreference>
{
    public void Configure(EntityTypeBuilder<UserCalendarPreference> builder)
    {
        builder.ToTable("user_calendar_preferences");

        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("user_id");

        builder.HasIndex(p => p.CalendarToken).IsUnique();

        builder.Property(p => p.CalendarToken).IsRequired().HasColumnName("calendar_token");
        builder.Property(p => p.ReminderMinutes).IsRequired().HasColumnName("reminder_minutes");
        builder.Property(p => p.PushEnabled).IsRequired().HasDefaultValue(false).HasColumnName("push_enabled");
        builder.Property(p => p.UpdatedAt).IsRequired().HasColumnName("updated_at");
    }
}
