using TTRPGHub.Entities;

namespace TTRPGHub.Common.Interfaces;

public interface IPushNotificationSender
{
    Task SendAsync(PushSubscription subscription, string title, string body, string? url, CancellationToken ct = default);
}
