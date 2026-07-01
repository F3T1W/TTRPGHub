using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Services;

internal sealed class SessionReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionReminderBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LookaheadWindow = TimeSpan.FromDays(3); // covers the largest reminder option (2 days)

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await CheckAndSendRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при проверке напоминаний о сессиях");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAndSendRemindersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var preferenceRepository = scope.ServiceProvider.GetRequiredService<IUserCalendarPreferenceRepository>();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<IPushSubscriptionRepository>();
        var reminderLogRepository = scope.ServiceProvider.GetRequiredService<ISessionReminderLogRepository>();
        var pushSender = scope.ServiceProvider.GetRequiredService<IPushNotificationSender>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var upcomingSessions = await sessionRepository.GetScheduledBetweenAsync(now, now.Add(LookaheadWindow), ct);

        foreach (var session in upcomingSessions)
        {
            if (session.Status != SessionStatus.Planned)
                continue;

            var alreadyNotified = await reminderLogRepository.GetNotifiedUserIdsAsync(session.Id, ct);

            foreach (var participant in session.Participants)
            {
                if (alreadyNotified.Contains(participant.UserId))
                    continue;

                var pref = await preferenceRepository.GetByUserIdAsync(participant.UserId, ct);
                if (pref is null || !pref.PushEnabled)
                    continue;

                var triggerAt = session.ScheduledAt.AddMinutes(-pref.ReminderMinutes);
                if (triggerAt > now)
                    continue; // not due yet

                var subscriptions = await subscriptionRepository.GetByUserIdAsync(participant.UserId, ct);
                if (subscriptions.Count == 0)
                    continue;

                foreach (var subscription in subscriptions)
                {
                    await pushSender.SendAsync(
                        subscription,
                        title: "Скоро игра!",
                        body: $"«{session.Title}» начнётся {session.ScheduledAt:dd.MM в HH:mm}",
                        url: $"/sessions/{session.Id.Value}",
                        ct);
                }

                await reminderLogRepository.AddAsync(SessionReminderLog.Create(session.Id, participant.UserId), ct);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
