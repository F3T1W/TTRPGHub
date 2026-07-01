using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTRPGHub.Common.Interfaces;
using WebPush;
using DomainPushSubscription = TTRPGHub.Entities.PushSubscription;

namespace TTRPGHub.Push;

internal sealed class WebPushNotificationSender(
    IOptions<VapidOptions> options,
    ILogger<WebPushNotificationSender> logger) : IPushNotificationSender
{
    private readonly VapidOptions _vapid = options.Value;

    public async Task SendAsync(DomainPushSubscription subscription, string title, string body, string? url, CancellationToken ct = default)
    {
        var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
        var vapidDetails = new VapidDetails(_vapid.Subject, _vapid.PublicKey, _vapid.PrivateKey);
        var client = new WebPushClient();

        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body, url });

        try
        {
            await client.SendNotificationAsync(pushSubscription, payload, vapidDetails, ct);
        }
        catch (WebPushException ex)
        {
            logger.LogWarning(ex, "Не удалось отправить push-уведомление на {Endpoint}", subscription.Endpoint);
        }
    }
}
