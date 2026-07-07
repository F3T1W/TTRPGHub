using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TTRPGHub.Hubs;

[Authorize]
public sealed class TableHub : Hub
{
    public async Task JoinTable(string sessionId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"table-{sessionId}");

    public async Task LeaveTable(string sessionId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"table-{sessionId}");

    // J.5 — шаблоны зон эффекта: эфемерные, не хранятся в БД (в отличие от стен/света), поэтому
    // ретранслируются напрямую через хаб, а не через REST-команду + ITableNotifier — авторизация
    // тут не нужна отдельно: любой участник стола (уже прошедший [Authorize] и JoinTable) может
    // разместить шаблон, как и с линейкой/измерением, это не изменение постоянного состояния сцены.
    public async Task BroadcastTemplate(string sessionId, string type, int feet, double originX, double originY, double angleDeg) =>
        await Clients.OthersInGroup(GroupName(Guid.Parse(sessionId)))
            .SendAsync("TemplatePlaced", type, feet, originX, originY, angleDeg);

    public async Task ClearTemplate(string sessionId) =>
        await Clients.OthersInGroup(GroupName(Guid.Parse(sessionId)))
            .SendAsync("TemplateCleared");

    // L.7 — общая линейка: эфемерное измерение, видимое всем участникам (как шаблоны зон).
    public async Task BroadcastMeasure(string sessionId, double x1, double y1, double x2, double y2, int feet) =>
        await Clients.OthersInGroup(GroupName(Guid.Parse(sessionId)))
            .SendAsync("MeasureDrawn", x1, y1, x2, y2, feet);

    // L.7 — пинг на карте: короткая анимация в точке клика, без персистентности.
    public async Task BroadcastPing(string sessionId, double x, double y) =>
        await Clients.OthersInGroup(GroupName(Guid.Parse(sessionId)))
            .SendAsync("PingPlaced", x, y);

    public static string GroupName(Guid sessionId) => $"table-{sessionId}";
}
