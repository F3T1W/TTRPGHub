namespace TTRPGHub.Entities;

public sealed class UserCalendarPreference
{
    public UserId UserId { get; private init; }
    public Guid CalendarToken { get; private set; }
    public int ReminderMinutes { get; private set; }
    public bool PushEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserCalendarPreference() { }

    public static UserCalendarPreference Create(UserId userId, int reminderMinutes)
        => new()
        {
            UserId = userId,
            CalendarToken = Guid.NewGuid(),
            ReminderMinutes = reminderMinutes,
            PushEnabled = false,
            UpdatedAt = DateTime.UtcNow
        };

    public void UpdateReminder(int reminderMinutes)
    {
        ReminderMinutes = reminderMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPushEnabled(bool enabled)
    {
        PushEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegenerateToken()
    {
        CalendarToken = Guid.NewGuid();
        UpdatedAt = DateTime.UtcNow;
    }
}
