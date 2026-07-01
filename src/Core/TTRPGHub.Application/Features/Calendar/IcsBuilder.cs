using System.Text;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Calendar;

internal static class IcsBuilder
{
    internal static string BuildFeed(IEnumerable<GameSession> sessions, int reminderMinutes)
    {
        var sb = new StringBuilder();
        AppendCalendarHeader(sb, "Таверна Аферистов — Игры");
        foreach (var s in sessions)
            AppendEvent(sb, s, reminderMinutes);
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    internal static string BuildSingleEvent(GameSession session, int reminderMinutes)
    {
        var sb = new StringBuilder();
        AppendCalendarHeader(sb, session.Title);
        AppendEvent(sb, session, reminderMinutes);
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static void AppendCalendarHeader(StringBuilder sb, string calName)
    {
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//TTRPGHub//Taverna//RU");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine($"X-WR-CALNAME:{Escape(calName)}");
        sb.AppendLine("X-WR-TIMEZONE:UTC");
    }

    private static void AppendEvent(StringBuilder sb, GameSession session, int reminderMinutes)
    {
        var start = session.ScheduledAt.ToUniversalTime();
        var end = start.AddHours(4);

        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{session.Id.Value}@ttrpghub");
        sb.AppendLine($"DTSTART:{FormatDate(start)}");
        sb.AppendLine($"DTEND:{FormatDate(end)}");
        sb.AppendLine($"SUMMARY:{Escape(session.System + ": " + session.Title)}");
        if (!string.IsNullOrEmpty(session.Description))
            sb.AppendLine($"DESCRIPTION:{Escape(session.Description)}");
        sb.AppendLine($"STATUS:{(session.Status == SessionStatus.Cancelled ? "CANCELLED" : "CONFIRMED")}");

        if (reminderMinutes > 0)
        {
            sb.AppendLine("BEGIN:VALARM");
            sb.AppendLine($"TRIGGER:-PT{reminderMinutes}M");
            sb.AppendLine("ACTION:DISPLAY");
            sb.AppendLine("DESCRIPTION:Скоро игра в Таверне Аферистов!");
            sb.AppendLine("END:VALARM");
        }

        sb.AppendLine("END:VEVENT");
    }

    private static string FormatDate(DateTime utc) => utc.ToString("yyyyMMddTHHmmssZ");

    // iCal requires escaping commas, semicolons, backslashes, and newlines
    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;")
         .Replace("\r\n", "\\n").Replace("\n", "\\n");
}
