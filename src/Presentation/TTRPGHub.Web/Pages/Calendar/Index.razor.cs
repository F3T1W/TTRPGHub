using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Calendar;

public partial class IndexBase : ComponentBase
{
    [Inject] protected IApiClient Api { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IJSRuntime Js { get; set; } = null!;
    [Inject] protected IConfiguration Configuration { get; set; } = null!;

    protected bool _loading = true;
    protected bool _saving;
    protected bool _saveSuccess;
    protected bool _copied;
    protected int _selectedMinutes = 60;
    protected Guid _token;

    protected bool _pushSupported = true;
    protected bool _pushEnabled;
    protected bool _pushBusy;
    protected string? _pushError;

    protected static readonly (string Label, int Minutes)[] ReminderOptions =
    [
        ("30 минут", 30),
        ("1 час", 60),
        ("2 часа", 120),
        ("3 часа", 180),
        ("12 часов", 720),
        ("1 день", 1440),
        ("2 дня", 2880),
    ];

    protected string WebcalUrl
    {
        get
        {
            var apiBase = ApiBaseUrl.Resolve(Configuration, Nav.BaseUri);
            var webcalBase = apiBase.Replace("http://", "webcal://").Replace("https://", "webcal://").TrimEnd('/');
            return $"{webcalBase}/api/calendar/feed/{_token}";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var pref = await Api.GetCalendarPreferenceAsync();
            _selectedMinutes = pref.ReminderMinutes;
            _token = pref.CalendarToken;
            _pushEnabled = pref.PushEnabled;
        }
        catch
        {
            // first visit — no preference yet, defaults apply
        }

        try
        {
            _pushSupported = await Js.InvokeAsync<bool>("pushNotifications.isSupported");
        }
        catch
        {
            _pushSupported = false;
        }

        _loading = false;
    }

    protected async Task SavePreferenceAsync()
    {
        _saving = true;
        _saveSuccess = false;
        try
        {
            var pref = await Api.UpsertCalendarPreferenceAsync(
                new UpsertCalendarPreferenceRequest(_selectedMinutes));
            _token = pref.CalendarToken;
            _saveSuccess = true;
            _ = Task.Delay(3000).ContinueWith(_ => { _saveSuccess = false; InvokeAsync(StateHasChanged); });
        }
        finally
        {
            _saving = false;
        }
    }

    protected async Task RegenerateTokenAsync()
    {
        var pref = await Api.UpsertCalendarPreferenceAsync(
            new UpsertCalendarPreferenceRequest(_selectedMinutes, RegenerateToken: true));
        _token = pref.CalendarToken;
    }

    protected async Task CopyWebcalUrl()
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", WebcalUrl);
        _copied = true;
        _ = Task.Delay(2000).ContinueWith(_ => { _copied = false; InvokeAsync(StateHasChanged); });
    }

    protected async Task TogglePushAsync(ChangeEventArgs e)
    {
        var enable = (bool)(e.Value ?? false);
        _pushBusy = true;
        _pushError = null;
        try
        {
            if (enable)
            {
                var vapidKey = await Api.GetVapidPublicKeyAsync();
                var sub = await Js.InvokeAsync<PushSubscriptionJs?>(
                    "pushNotifications.requestPermissionAndSubscribe", vapidKey);

                if (sub is null)
                {
                    _pushError = "Не удалось получить разрешение на уведомления.";
                    _pushEnabled = false;
                    return;
                }

                await Api.SubscribePushAsync(new SubscribePushRequest(sub.Endpoint, sub.P256dh, sub.Auth));
                _pushEnabled = true;
            }
            else
            {
                var sub = await Js.InvokeAsync<PushSubscriptionJs?>("pushNotifications.unsubscribe");
                if (sub is not null)
                    await Api.UnsubscribePushAsync(new UnsubscribePushRequest(sub.Endpoint));
                _pushEnabled = false;
            }
        }
        catch
        {
            _pushError = "Не удалось изменить настройки push-уведомлений.";
        }
        finally
        {
            _pushBusy = false;
        }
    }

    protected sealed record PushSubscriptionJs(string Endpoint, string P256dh, string Auth);
}
