using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Sessions;

public partial class Table : ComponentBase, IAsyncDisposable
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private TokenStorage TokenStorage { get; set; } = default!;

    private TableStateDto? _state;
    private readonly List<TableMessageDto> _messages = [];
    private HubConnection? _hub;
    private ElementReference _chatLogRef;

    private bool _loading = true;
    private string? _error;
    private string _connectionStatus = "disconnected";

    private string _chatInput = string.Empty;
    private string _customExpression = string.Empty;
    private bool _rolling;
    private string? _rollError;
    private string? _uploadError;

    private Guid? _whisperTargetId;
    private string _whisperInput = string.Empty;

    private string _trackUrlInput = string.Empty;
    private string? _audioError;
    private double _volume = 0.8;

    private readonly List<TableTokenDto> _tokens = [];
    private bool _showAddToken;
    private string _newTokenLabel = string.Empty;
    private string _newTokenOwnerId = string.Empty;
    private DotNetObjectReference<Table>? _dotNetRef;
    private bool _dragInitialized;
    private Guid? _currentUserId;

    private static readonly string[] QuickDice = ["d4", "d6", "d8", "d10", "d12", "d20", "d100"];
    private static readonly string[] TokenColors = ["#7c3aed", "#0d9488", "#b45309", "#1d4ed8", "#be185d", "#dc2626"];

    // CSS требует точку как десятичный разделитель; CurrentCulture (например, ru-RU)
    // подставляет запятую, что делает значение left/top невалидным CSS и браузер
    // молча отбрасывает всё объявление — отсюда "магнит" к целым процентам (0/50/100).
    private static string Pct(double fraction) =>
        (fraction * 100).ToString(CultureInfo.InvariantCulture);

    private static string Inv(double value) => value.ToString(CultureInfo.InvariantCulture);

    protected override async Task OnInitializedAsync()
    {
        _currentUserId = await TokenStorage.GetUserIdAsync();

        try
        {
            _state = await Api.GetTableStateAsync(Id);
            _messages.AddRange(_state.RecentMessages);
            _tokens.AddRange(_state.Tokens);
        }
        catch (Exception ex)
        {
            _error = ex.Message.Contains("404") ? "Сессия не найдена." : "Стол недоступен. Игра должна быть в статусе «Идёт».";
        }
        finally
        {
            _loading = false;
        }

        if (_state is not null)
        {
            await ConnectHubAsync();
            await SyncAudioPlayerAsync(_state.Audio);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Не привязываемся к firstRender: тот рендер показывает спиннер загрузки,
        // а контейнер карты появляется в DOM только после того, как подгрузится _state.
        if (_state is not null && !_dragInitialized)
        {
            _dragInitialized = true;
            _dotNetRef = DotNetObjectReference.Create(this);
            try { await Js.InvokeVoidAsync("tableDrag.init", "table-showcase-container", _dotNetRef); }
            catch { /* ignore */ }
        }
    }

    private async Task ConnectHubAsync()
    {
        var apiBase = Nav.BaseUri.TrimEnd('/').Replace(":5141", ":5014");
        _hub = new HubConnectionBuilder()
            .WithUrl($"{apiBase}/hubs/table", options =>
            {
                options.AccessTokenProvider = () => TokenStorage.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<TableMessageDto>("TableMessageReceived", async msg =>
        {
            _messages.Add(msg);
            await InvokeAsync(StateHasChanged);
            await ScrollChatToBottomAsync();
        });

        _hub.On<string?>("ShowcaseImageChanged", async url =>
        {
            if (_state is not null)
                _state = _state with { ShowcaseImageUrl = url };
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<AudioStateDto>("AudioStateChanged", async audio =>
        {
            if (_state is not null)
                _state = _state with { Audio = audio };
            await InvokeAsync(StateHasChanged);
            await SyncAudioPlayerAsync(audio);
        });

        _hub.On<TableTokenDto>("TokenAdded", async dto =>
        {
            var canMove = _state?.IsOrganizer == true || dto.OwnerId == _currentUserId;
            _tokens.Add(dto with { CanMove = canMove });
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<Guid, double, double>("TokenMoved", async (tokenId, x, y) =>
        {
            var idx = _tokens.FindIndex(t => t.Id == tokenId);
            if (idx >= 0) _tokens[idx] = _tokens[idx] with { X = x, Y = y };
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<Guid>("TokenRemoved", async tokenId =>
        {
            _tokens.RemoveAll(t => t.Id == tokenId);
            await InvokeAsync(StateHasChanged);
        });

        _hub.Reconnecting += _ => { _connectionStatus = "reconnecting"; return InvokeAsync(StateHasChanged); };
        _hub.Reconnected += _ => { _connectionStatus = "connected"; return InvokeAsync(StateHasChanged); };
        _hub.Closed += _ => { _connectionStatus = "disconnected"; return InvokeAsync(StateHasChanged); };

        try
        {
            await _hub.StartAsync();
            await _hub.InvokeAsync("JoinTable", Id.ToString());
            _connectionStatus = "connected";
        }
        catch
        {
            _connectionStatus = "disconnected";
        }
    }

    private async Task SendChatAsync()
    {
        if (string.IsNullOrWhiteSpace(_chatInput)) return;
        var content = _chatInput;
        _chatInput = string.Empty;
        try { await Api.SendTableChatAsync(Id, new SendChatRequest(content)); }
        catch { _chatInput = content; }
    }

    private async Task OnChatKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await SendChatAsync();
    }

    private async Task RollAsync(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return;
        _rolling = true;
        _rollError = null;
        try
        {
            await Api.RollTableDiceAsync(Id, new RollDiceRequest(expression));
            if (expression == _customExpression) _customExpression = string.Empty;
        }
        catch
        {
            _rollError = "Не удалось разобрать формулу броска.";
        }
        finally
        {
            _rolling = false;
        }
    }

    private async Task OnCustomDiceKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await RollAsync(_customExpression);
    }

    private async Task OnShowcaseFileSelected(InputFileChangeEventArgs e)
    {
        _uploadError = null;
        var file = e.File;
        if (file.Size > 10 * 1024 * 1024)
        {
            _uploadError = "Максимальный размер файла — 10 МБ.";
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream(10 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            var streamPart = new Refit.StreamPart(ms, file.Name, file.ContentType);
            await Api.UploadTableShowcaseAsync(Id, streamPart);
        }
        catch
        {
            _uploadError = "Не удалось загрузить изображение.";
        }
    }

    private void ToggleWhisperTarget(Guid userId)
    {
        _whisperTargetId = _whisperTargetId == userId ? null : userId;
        _whisperInput = string.Empty;
    }

    private async Task SendWhisperAsync(Guid recipientId)
    {
        if (string.IsNullOrWhiteSpace(_whisperInput)) return;
        var content = _whisperInput;
        _whisperInput = string.Empty;
        try { await Api.SendTableWhisperAsync(Id, new SendWhisperRequest(recipientId, content)); }
        catch { _whisperInput = content; }
    }

    private async Task OnWhisperKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && _whisperTargetId is { } targetId)
            await SendWhisperAsync(targetId);
    }

    private async Task ClearShowcaseAsync()
    {
        try { await Api.SetTableShowcaseAsync(Id, new SetShowcaseRequest(null)); }
        catch { /* ignore */ }
    }

    private async Task SyncAudioPlayerAsync(AudioStateDto audio)
    {
        if (audio.TrackUrl is null)
        {
            try { await Js.InvokeVoidAsync("tableAudio.stop"); } catch { /* ignore */ }
            return;
        }

        var elapsed = audio.IsPlaying ? (DateTime.UtcNow - audio.ServerTimestamp).TotalSeconds : 0;
        var targetPosition = audio.PositionSeconds + elapsed;

        try { await Js.InvokeVoidAsync("tableAudio.sync", audio.TrackUrl, audio.IsPlaying, targetPosition); }
        catch { /* ignore */ }
    }

    private async Task OnTrackFileSelected(InputFileChangeEventArgs e)
    {
        _audioError = null;
        var file = e.File;
        if (file.Size > 30 * 1024 * 1024)
        {
            _audioError = "Максимальный размер файла — 30 МБ.";
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream(30 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            var streamPart = new Refit.StreamPart(ms, file.Name, file.ContentType);
            await Api.UploadTableTrackAsync(Id, streamPart);
        }
        catch
        {
            _audioError = "Не удалось загрузить трек.";
        }
    }

    private async Task SetTrackByUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(_trackUrlInput)) return;
        var url = _trackUrlInput;
        _trackUrlInput = string.Empty;
        try { await Api.SetTableTrackAsync(Id, new SetTrackRequest(url, null)); }
        catch { _audioError = "Не удалось установить трек."; }
    }

    private async Task PlayAsync()
    {
        try
        {
            var position = await Js.InvokeAsync<double>("tableAudio.getCurrentTime");
            await Api.PlayTableAudioAsync(Id, new AudioPositionRequest(position));
        }
        catch { _audioError = "Не удалось начать воспроизведение."; }
    }

    private async Task PauseAsync()
    {
        try
        {
            var position = await Js.InvokeAsync<double>("tableAudio.getCurrentTime");
            await Api.PauseTableAudioAsync(Id, new AudioPositionRequest(position));
        }
        catch { _audioError = "Не удалось поставить на паузу."; }
    }

    private async Task ClearAudioAsync()
    {
        try { await Api.ClearTableAudioAsync(Id); }
        catch { /* ignore */ }
    }

    private async Task OnVolumeChanged(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out var volume))
        {
            _volume = volume;
            try { await Js.InvokeVoidAsync("tableAudio.setVolume", volume); } catch { /* ignore */ }
        }
    }

    private async Task AddTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(_newTokenLabel)) return;

        Guid? ownerId = Guid.TryParse(_newTokenOwnerId, out var parsed) ? parsed : null;
        var owner = _state?.Participants.FirstOrDefault(p => p.UserId == ownerId);
        var color = TokenColors[_tokens.Count % TokenColors.Length];

        try
        {
            await Api.AddTableTokenAsync(Id, new AddTokenRequest(_newTokenLabel.Trim(), owner?.AvatarUrl, color, 0.5, 0.5, ownerId));
            _newTokenLabel = string.Empty;
            _newTokenOwnerId = string.Empty;
            _showAddToken = false;
        }
        catch { /* ignore */ }
    }

    private async Task RemoveTokenAsync(Guid tokenId)
    {
        try { await Api.RemoveTableTokenAsync(Id, tokenId); }
        catch { /* ignore */ }
    }

    [JSInvokable]
    public async Task OnTokenDragEnd(string tokenId, double x, double y)
    {
        if (!Guid.TryParse(tokenId, out var id)) return;

        var idx = _tokens.FindIndex(t => t.Id == id);
        if (idx >= 0) _tokens[idx] = _tokens[idx] with { X = x, Y = y };
        StateHasChanged();

        try { await Api.MoveTableTokenAsync(Id, id, new TokenPositionRequest(x, y)); }
        catch { /* ignore */ }
    }

    private async Task ScrollChatToBottomAsync()
    {
        try { await Js.InvokeVoidAsync("tableHelpers.scrollToBottom", _chatLogRef); }
        catch { /* ignore */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            if (_hub.State == HubConnectionState.Connected)
                await _hub.InvokeAsync("LeaveTable", Id.ToString());
            await _hub.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }
}
