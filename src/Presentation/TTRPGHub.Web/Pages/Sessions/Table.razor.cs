using System.Globalization;
using System.Text.Json;
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
    [Inject] private IConfiguration Config { get; set; } = default!;
    [Inject] private Pf2eLocaleService Locale { get; set; } = default!;
    [Inject] private ContentLanguageService Lang { get; set; } = default!;

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
    private int? _checkDc;
    private string _checkLabel = string.Empty;
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

    private List<SessionCharacterDto> _sessionCharacters = [];
    private bool _showAddCombatant;
    private string _monsterSearch = string.Empty;
    private List<Pf2eMonsterSummaryDto> _monsterResults = [];
    private Dictionary<Guid, Pf2eLocalizedMonsterRow> _monsterLocalized = [];
    private bool _searchingMonsters;
    private int _gridCellSizePx = 50;
    private bool _fogEnabled;
    private int _visionRadiusFeet = 30;
    private bool _wallMode;
    private List<WallDto> _walls = [];
    private static readonly JsonSerializerOptions WallsJsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record WallDto(double X1, double Y1, double X2, double Y2, bool IsDoor = false, bool IsOpen = false);

    // J.3 — источники света сцены, независимо от стен/тумана выше.
    private bool _lightMode;
    private List<LightDto> _lights = [];
    private sealed record LightDto(double X, double Y, int BrightRadiusFeet, int DimRadiusFeet, string Color);
    private static readonly string[] LightColors = ["#f59e0b", "#7c3aed", "#0ea5e9", "#22c55e", "#ffffff"];
    // J.4 — множественные сцены на сессию.
    private bool _showSceneManager;
    private string _newSceneName = string.Empty;

    private bool _measureMode;
    private bool _pingMode;
    private bool _doorDrawMode;
    private bool _templateMode;
    private string _templateType = "burst";
    private int _templateFeet = 15;

    // J.5 — размещённый шаблон (свой или полученный от другого участника по SignalR) и набор
    // задетых им токенов — вычисляется один раз при размещении/получении, не пересчитывается на
    // каждый рендер (шаблон статичен, пока его явно не уберут/не заменят новым).
    private sealed record PlacedTemplate(string Type, int Feet, double OriginX, double OriginY, double AngleDeg);
    private PlacedTemplate? _placedTemplate;
    private HashSet<Guid> _templateAffectedTokenIds = [];
    private Guid? _selectedTokenId;
    private List<RuleEntrySummaryDto> _pf2eConditions = [];
    private Dictionary<string, string> _conditionTitles = new(StringComparer.OrdinalIgnoreCase);
    private string _newConditionSlug = string.Empty;
    private int? _newConditionValue;

    private HashSet<Guid> _flankedTokenIds = [];
    private int? _saveTargetDc;
    private Pf2eLookups.Pf2eStatsModel? _selectedCharacterStats;
    private int _selectedCharacterLevel;
    private Dictionary<string, int> _selectedCharacterAbilityMods = [];
    private List<Pf2eLookups.Pf2eMonsterAttack> _selectedMonsterAttacks = [];
    private int _selectedMonsterLevel;
    private int _selectedMonsterFort;
    private int _selectedMonsterReflex;
    private int _selectedMonsterWill;
    private int _selectedMonsterAbilityDc;

    // L.1 — MAP: число уже совершённых ударов в текущем ходу по токену; сбрасывается, когда
    // CombatTurnTokenId переключается на этот токен (начало его хода).
    private readonly Dictionary<Guid, int> _strikesThisTurn = new();

    private List<JournalEntryDto> _journalEntries = [];
    private Guid? _editingJournalEntryId;
    private string _journalTitleInput = string.Empty;
    private string _journalContentInput = string.Empty;
    private string? _journalError;
    private Guid? _journalParentId;
    private Guid? _journalCampaignId;

    // L.3 — встроенный PF2e-лист (slide-over), без ухода со страницы стола.
    private bool _showPf2eSheet;
    private CharacterDetailDto? _sheetCharacter;

    private static readonly string[] QuickDice = ["d4", "d6", "d8", "d10", "d12", "d20", "d100"];
    private static readonly string[] TokenColors = ["#7c3aed", "#0d9488", "#b45309", "#1d4ed8", "#be185d", "#dc2626"];

    // CSS требует точку как десятичный разделитель; CurrentCulture (например, ru-RU)
    // подставляет запятую, что делает значение left/top невалидным CSS и браузер
    // молча отбрасывает всё объявление — отсюда "магнит" к целым процентам (0/50/100).
    private static string Inv(double value) => value.ToString(CultureInfo.InvariantCulture);

    private string TokenLeftPx(TableTokenDto t) => Inv(t.X * _gridCellSizePx);
    private string TokenTopPx(TableTokenDto t) => Inv(t.Y * _gridCellSizePx);
    private string TokenWidthPx(TableTokenDto t) => Inv(t.Width * _gridCellSizePx);
    private string TokenHeightPx(TableTokenDto t) => Inv(t.Height * _gridCellSizePx);

    protected override async Task OnInitializedAsync()
    {
        await Lang.InitializeAsync();
        Lang.OnChanged += OnContentLanguageChanged;
        _currentUserId = await TokenStorage.GetUserIdAsync();

        try
        {
            await LoadTableStateAsync();
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

            if (_state.IsOrganizer)
            {
                try { _sessionCharacters = await Api.GetSessionCharactersAsync(Id); }
                catch { /* ignore */ }
            }

            // Список состояний PF2e для быстрого выбора при наложении на жетон — если стол ведётся
            // не по PF2e, список останется пустым, и GM/игрок сможет ввести название вручную.
            try
            {
                var page = await Api.GetRuleEntriesAsync("pf2e", "condition", pageSize: 50);
                _pf2eConditions = page.Items;
                await BuildConditionTitlesAsync();
            }
            catch { /* ignore */ }

            try { _journalEntries = await Api.GetJournalEntriesAsync(Id); }
            catch { /* ignore */ }
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
            try { await Js.InvokeVoidAsync("tableDrag.init", "table-showcase-container", _dotNetRef, _gridCellSizePx); }
            catch { /* ignore */ }
        }

        if (_state is not null)
            await UpdateFogVisualizationAsync();
    }

    // J.4 — полная перезагрузка состояния активной сцены: вызывается при первой загрузке страницы
    // и при любом переключении/изменении списка сцен (см. "ActiveSceneChanged" в ConnectHubAsync).
    // Проще перезапросить всё целиком (карта/токены/туман/стены/свет/бой у каждой сцены
    // независимы), чем присылать diff по каждому полю через SignalR.
    private async Task LoadTableStateAsync()
    {
        _state = await Api.GetTableStateAsync(Id);
        _tokens.Clear();
        _tokens.AddRange(_state.Tokens);
        _gridCellSizePx = _state.GridCellSizePx;
        _fogEnabled = _state.FogEnabled;
        _visionRadiusFeet = _state.VisionRadiusFeet;
        _walls = ParseWalls(_state.WallsJson);
        _lights = ParseLights(_state.LightsJson);
        _terrainTags = Pf2eLookups.ParseTerrainTags(_state.TerrainTagsJson);
        _ambientLighting = _state.AmbientLighting;
        _selectedTerrainTag = _terrainTags.FirstOrDefault() ?? "";
        _selectedTokenId = null;
        _wallMode = false;
        _lightMode = false;
        _placedTemplate = null;
        _templateAffectedTokenIds = [];

        if (_messages.Count == 0)
            _messages.AddRange(_state.RecentMessages);

        RecomputeFlanking();
    }

    private static List<WallDto> ParseWalls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<WallDto>>(json, WallsJsonOptions) ?? []; }
        catch { return []; }
    }

    private static List<LightDto> ParseLights(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<LightDto>>(json, WallsJsonOptions) ?? []; }
        catch { return []; }
    }

    // Перерисовываем туман на каждый рендер (после любого StateHasChanged — движение токена,
    // изменение стен, переключение тумана) — дешёвая операция в JS, не требует точечной
    // подписки на каждое конкретное событие, которое могло бы повлиять на видимость.
    private async Task UpdateFogVisualizationAsync()
    {
        if (_state is null) return;

        // GM всегда видит линии стен (чтобы понимать геометрию, которую рисует), независимо
        // от того, включён ли туман для игроков — это разные вещи. Свечение источников света
        // (J.3) видно всем всегда — это не туман, а обстановка сцены.
        try { await Js.InvokeVoidAsync("tableDrag.renderWalls", _walls, _state.IsOrganizer); } catch { /* ignore */ }
        try { await Js.InvokeVoidAsync("tableDrag.renderLights", _lights); } catch { /* ignore */ }

        var visible = _fogEnabled && !_state.IsOrganizer;
        if (!visible)
        {
            try { await Js.InvokeVoidAsync("tableDrag.clearFog"); } catch { /* ignore */ }
            return;
        }

        var origins = _tokens
            .Where(t => t.OwnerId == _currentUserId)
            .Select(t => new { x = t.X + t.Width / 2.0, y = t.Y + t.Height / 2.0, darkvision = t.HasDarkvision, lowLight = t.HasLowLightVision })
            .ToList();

        try { await Js.InvokeVoidAsync("tableDrag.updateFog", true, origins, _walls, _visionRadiusFeet, _lights, _ambientLighting); }
        catch { /* ignore */ }
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
            // K.4 — звук кубиков всем за столом при любом броске (не только своём) — как в
            // Foundry, где звук слышат все участники, а не только бросающий.
            if (msg.Kind == TableMessageKind.Roll)
                try { await Js.InvokeVoidAsync("tableDrag.playDiceSound"); } catch { /* ignore */ }
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
            if (_placedTemplate is not null) RecomputeTemplateAffectedTokens();
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<string, int, double, double, double>("TemplatePlaced", async (type, feet, originX, originY, angleDeg) =>
        {
            _placedTemplate = new PlacedTemplate(type, feet, originX, originY, angleDeg);
            RecomputeTemplateAffectedTokens();
            try { await Js.InvokeVoidAsync("tableDrag.showTemplate", type, feet, originX, originY, angleDeg); } catch { /* ignore */ }
            await InvokeAsync(StateHasChanged);
        });

        _hub.On("TemplateCleared", async () =>
        {
            _placedTemplate = null;
            _templateAffectedTokenIds = [];
            try { await Js.InvokeVoidAsync("tableDrag.clearPlacedTemplate"); } catch { /* ignore */ }
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<Guid>("TokenRemoved", async tokenId =>
        {
            _tokens.RemoveAll(t => t.Id == tokenId);
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<TableTokenDto>("TokenUpdated", async dto =>
        {
            var idx = _tokens.FindIndex(t => t.Id == dto.Id);
            if (idx >= 0)
            {
                var previousHp = _tokens[idx].CurrentHp;
                var canMove = _tokens[idx].CanMove;
                _tokens[idx] = dto with { CanMove = canMove };

                // K.4 — вспышка урона/лечения должна быть видна и наблюдателям (GM бьёт монстра —
                // игроки видят красный пульс на его токене), не только тому, кто нажал кнопку —
                // тот уже получил вспышку оптимистично в AdjustHpAsync/ApplyDamageAsync.
                if (previousHp is { } prev && dto.CurrentHp is { } next && next != prev)
                    _ = FlashHpChangeAsync(dto.Id, next - prev);
            }
            // K.2 — на жетоне могло появиться новое состояние, формулы которого ещё не в кеше.
            await InvokeAsync(EnsureConditionStatsAsync);
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<int>("GridCellSizeChanged", async px =>
        {
            _gridCellSizePx = px;
            try { await Js.InvokeVoidAsync("tableDrag.setCellSize", px); } catch { /* ignore */ }
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<bool, int>("FogSettingsChanged", async (enabled, radiusFeet) =>
        {
            _fogEnabled = enabled;
            _visionRadiusFeet = radiusFeet;
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<string?, string>("SceneEnvironmentChanged", async (terrainTagsJson, ambientLighting) =>
        {
            _terrainTags = Pf2eLookups.ParseTerrainTags(terrainTagsJson);
            _selectedTerrainTag = _terrainTags.FirstOrDefault() ?? "";
            _ambientLighting = ambientLighting;
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<bool, int, Guid?>("CombatStateChanged", async (active, round, turnTokenId) =>
        {
            if (_state is not null)
                _state = _state with { CombatActive = active, CombatRound = round, CombatTurnTokenId = turnTokenId };

            if (!active)
                _strikesThisTurn.Clear();
            else if (turnTokenId is Guid tokenId)
                _strikesThisTurn.Remove(tokenId);

            await InvokeAsync(StateHasChanged);
        });

        _hub.On<string?>("LightsChanged", async lightsJson =>
        {
            _lights = ParseLights(lightsJson);
            await InvokeAsync(StateHasChanged);
        });

        _hub.On("ActiveSceneChanged", async () =>
        {
            try { await LoadTableStateAsync(); } catch { /* ignore */ }
            try { await Js.InvokeVoidAsync("tableDrag.setCellSize", _gridCellSizePx); } catch { /* ignore */ }
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<JournalEntryDto>("JournalEntryChanged", async entry =>
        {
            if (!IsJournalVisibleToMe(entry)) return;
            var idx = _journalEntries.FindIndex(e => e.Id == entry.Id);
            if (idx >= 0) _journalEntries[idx] = entry;
            else _journalEntries.Insert(0, entry);
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<Guid>("JournalEntryRemoved", async entryId =>
        {
            _journalEntries.RemoveAll(e => e.Id == entryId);
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<double, double, double, double, int>("MeasureDrawn", async (x1, y1, x2, y2, feet) =>
        {
            try { await Js.InvokeVoidAsync("tableDrag.showRemoteMeasure", x1, y1, x2, y2, feet); } catch { /* ignore */ }
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<double, double>("PingPlaced", async (x, y) =>
        {
            try { await Js.InvokeVoidAsync("tableDrag.showPing", x, y); } catch { /* ignore */ }
            await InvokeAsync(StateHasChanged);
        });

        _hub.On<string?>("WallsChanged", async wallsJson =>
        {
            _walls = ParseWalls(wallsJson);
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

    private async Task RollCheckAsync()
    {
        if (string.IsNullOrWhiteSpace(_customExpression) || _checkDc is null) return;
        _rolling = true;
        _rollError = null;
        try
        {
            await Api.RollTableDiceAsync(Id, new RollDiceRequest(
                _customExpression, _checkDc,
                string.IsNullOrWhiteSpace(_checkLabel) ? null : _checkLabel));
            _customExpression = string.Empty;
        }
        catch
        {
            _rollError = "Для проверки нужен ровно один d20 в формуле, например: 1d20+7.";
        }
        finally
        {
            _rolling = false;
        }
    }

    // Степень успеха приходит уже вшитой в текст сообщения (RollDiceCommandHandler), здесь
    // просто подсвечиваем строку по ключевым словам, не дублируя парсинг DegreeOfSuccess на клиенте.
    private static string DegreeStyle(string content) => content switch
    {
        _ when content.Contains("Критический успех") => "color:#22c55e;font-weight:600;",
        _ when content.Contains("Критический провал") => "color:#ef4444;font-weight:600;",
        _ when content.Contains("→ Успех") => "color:#a3e635;",
        _ when content.Contains("→ Провал") => "color:#fb923c;",
        _ => ""
    };

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
            await Api.AddTableTokenAsync(Id, new AddTokenRequest(_newTokenLabel.Trim(), owner?.AvatarUrl, color, 5, 5, ownerId));
            _newTokenLabel = string.Empty;
            _newTokenOwnerId = string.Empty;
            _showAddToken = false;
        }
        catch { /* ignore */ }
    }

    private async Task AddCharacterTokenAsync(SessionCharacterDto c)
    {
        var color = TokenColors[_tokens.Count % TokenColors.Length];
        try
        {
            await Api.AddTableTokenAsync(Id, new AddTokenRequest(
                c.Name, c.AvatarUrl, color, 5, 5, c.OwnerId,
                Width: 1, Height: 1, CombatantType: "Character", CombatantId: c.Id));
        }
        catch { /* ignore */ }
    }

    private async Task SearchMonstersAsync()
    {
        _searchingMonsters = true;
        try
        {
            var page = await Api.GetPf2eMonstersAsync(search: _monsterSearch, pageSize: 15);
            _monsterResults = page.Items;
            await LocalizeMonsterResultsAsync();
        }
        catch { _monsterResults = []; _monsterLocalized = []; }
        finally { _searchingMonsters = false; }
    }

    private async Task LocalizeMonsterResultsAsync()
    {
        _monsterLocalized = new Dictionary<Guid, Pf2eLocalizedMonsterRow>();
        foreach (var m in _monsterResults)
            _monsterLocalized[m.Id] = await Locale.LocalizeAsync(m);
    }

    private Pf2eLocalizedMonsterRow MonsterDisplay(Pf2eMonsterSummaryDto m) =>
        _monsterLocalized.GetValueOrDefault(m.Id, new Pf2eLocalizedMonsterRow(m.Name, m.Traits, m.Size));

    private async Task AddMonsterTokenAsync(Pf2eMonsterSummaryDto m)
    {
        var color = TokenColors[_tokens.Count % TokenColors.Length];
        // K.3 — уникальный генерируемый арт по данным монстра (см. Pf2eTokenArtGenerator в API);
        // статичные плейсхолдеры по типу существа (J.6, Pf2eLookups.MonsterPlaceholderIcon)
        // остались фолбэком для жетонов без привязки к бестиарию.
        var apiBase = Config["ApiBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5014";
        var label = MonsterDisplay(m).Name;
        try
        {
            await Api.AddTableTokenAsync(Id, new AddTokenRequest(
                label, $"{apiBase}/api/v1/pf2e/monsters/{m.Id}/token.svg", color, 5, 5, null,
                Width: SizeToCells(m.Size), Height: SizeToCells(m.Size),
                CombatantType: "Pf2eMonster", CombatantId: m.Id));
        }
        catch { /* ignore */ }
    }

    private static int SizeToCells(string? size) => size?.ToLowerInvariant() switch
    {
        "large" or "большой" => 2,
        "huge" or "огромный" => 3,
        "gargantuan" or "исполинский" => 4,
        _ => 1
    };

    private void SelectToken(Guid tokenId)
    {
        _selectedTokenId = _selectedTokenId == tokenId ? null : tokenId;
        _newConditionSlug = string.Empty;
        _newConditionValue = null;
        _selectedCharacterStats = null;
        _selectedMonsterAttacks = [];
        _selectedMonsterResistances = [];
        _selectedMonsterWeaknesses = [];
        _damageAmount = null;
        _damageType = string.Empty;
        _targetTokenId = null;
        _selectedActionSlug = string.Empty;
        _selectedFeatModifiers = [];

        var token = _tokens.FirstOrDefault(t => t.Id == tokenId);
        if (_selectedTokenId is null) return;

        if (token is { CombatantType: "Character", CombatantId: { } characterId })
            _ = LoadSelectedCharacterStatsAsync(characterId);
        else if (token is { CombatantType: "Pf2eMonster", CombatantId: { } monsterId })
            _ = LoadSelectedMonsterAttacksAsync(monsterId);
    }

    private async Task LoadSelectedMonsterAttacksAsync(Guid monsterId)
    {
        try
        {
            var monster = await Api.GetPf2eMonsterAsync(monsterId);
            _selectedMonsterAttacks = Pf2eLookups.ParseMonsterAttacks(monster.AttacksJson);
            _selectedMonsterResistances = Pf2eLookups.ParseDamageAdjustments(monster.ResistancesJson);
            _selectedMonsterWeaknesses = Pf2eLookups.ParseDamageAdjustments(monster.WeaknessesJson);
            _selectedMonsterLevel = monster.Level;
            _selectedMonsterFort = monster.Fortitude;
            _selectedMonsterReflex = monster.Reflex;
            _selectedMonsterWill = monster.Will;
            _selectedMonsterAbilityDc = Pf2eLookups.MonsterAbilityDc(
                monster.Level, monster.Intelligence, monster.Wisdom, monster.Charisma);
            if (_saveTargetDc is null) _saveTargetDc = _selectedMonsterAbilityDc;
            StateHasChanged();
        }
        catch { _selectedMonsterAttacks = []; }
    }

    // J.2 (combat tracker "до идеального состояния") — урон с учётом сопротивлений/уязвимостей
    // монстра. GM вводит сырой урон и выбирает тип — считаем эффективный урон по спискам
    // ResistancesJson/WeaknessesJson выбранного токена-монстра и применяем как обычный
    // AdjustHpAsync (тот же путь, что и кнопки ±1/±5, чтобы не дублировать синхронизацию с
    // персонажем/SignalR).
    private int? _damageAmount;
    private string _damageType = string.Empty;
    private List<Pf2eLookups.Pf2eDamageAdjustment> _selectedMonsterResistances = [];
    private List<Pf2eLookups.Pf2eDamageAdjustment> _selectedMonsterWeaknesses = [];

    private static readonly string[] DamageTypes =
    [
        "bludgeoning", "piercing", "slashing", "acid", "cold", "electricity",
        "fire", "sonic", "poison", "mental", "force", "spirit", "void", "vitality", "physical",
    ];

    private async Task ApplyDamageAsync(TableTokenDto token)
    {
        if (_damageAmount is not { } raw || raw <= 0) return;

        var resistance = _selectedMonsterResistances.FirstOrDefault(r => r.Type == _damageType)?.Value;
        var weakness = _selectedMonsterWeaknesses.FirstOrDefault(w => w.Type == _damageType)?.Value;
        var effective = Pf2eLookups.ApplyDamageAdjustment(raw, resistance, weakness);

        await AdjustHpAsync(token, -effective);
        _damageAmount = null;
    }

    private async Task LoadSelectedCharacterStatsAsync(Guid characterId)
    {
        try
        {
            var character = await Api.GetCharacterByIdAsync(characterId);
            _selectedCharacterLevel = character.Level;
            _selectedCharacterAbilityMods = new Dictionary<string, int>
            {
                ["str"] = character.StrengthModifier, ["dex"] = character.DexterityModifier,
                ["con"] = character.ConstitutionModifier, ["int"] = character.IntelligenceModifier,
                ["wis"] = character.WisdomModifier, ["cha"] = character.CharismaModifier
            };
            _selectedCharacterRace = character.Race;
            _selectedCharacterStats = string.IsNullOrWhiteSpace(character.Pf2eStatsJson)
                ? null
                : Pf2eLookups.Pf2eStatsModel.FromJson(character.Pf2eStatsJson);
            await LoadCombatModifierDataAsync();
            StateHasChanged();
        }
        catch { _selectedCharacterStats = null; }
    }

    private async Task OpenPf2eSheetAsync(TableTokenDto token)
    {
        if (token.CombatantType != "Character" || token.CombatantId is not Guid charId)
            return;

        try
        {
            var character = await Api.GetCharacterByIdAsync(charId);
            if (string.IsNullOrWhiteSpace(character.Pf2eStatsJson)
                && character.OwnerId != _currentUserId)
                return;

            _sheetCharacter = character;
            _showPf2eSheet = true;
        }
        catch { /* ignore */ }
    }

    private void ClosePf2eSheet()
    {
        _showPf2eSheet = false;
        _sheetCharacter = null;
    }

    private async Task OnSheetCharacterUpdatedAsync(CharacterDetailDto character)
    {
        _sheetCharacter = character;
        if (SelectedToken?.CombatantId == character.Id)
            await LoadSelectedCharacterStatsAsync(character.Id);
    }

    // ── K.2 — боевой контекст предикатного движка ────────────────────────────────
    // Модификаторы фитов (с предикатами) и состояний применяются к броскам за столом с учётом
    // контекста: состояния своего жетона (self:condition:X), идущий бой (encounter), выбранная
    // цель и её трейты (target:trait:undead → работают фиты вида «+2 против нежити»).

    // K.4 — короткая цветовая вспышка на жетоне при получении урона/лечения: slug класса, а не
    // просто bool, чтобы различать красный/зелёный. Ключ убирается сам через задержку — обычный
    // C#-таймер, а не JS: не нужно синхронизировать состояние обратно, класс живёт только в DOM.
    private readonly Dictionary<Guid, string> _hpFlashByTokenId = [];

    private string HpFlashClass(Guid tokenId) => _hpFlashByTokenId.GetValueOrDefault(tokenId, "");

    private async Task FlashHpChangeAsync(Guid tokenId, int delta)
    {
        if (delta == 0) return;
        _hpFlashByTokenId[tokenId] = delta < 0 ? "ta-token-damage-flash" : "ta-token-heal-flash";
        await InvokeAsync(StateHasChanged);
        await Task.Delay(650);
        _hpFlashByTokenId.Remove(tokenId);
        await InvokeAsync(StateHasChanged);
    }

    private Guid? _targetTokenId;
    private string _selectedActionSlug = string.Empty;
    private string? _selectedCharacterRace;
    private List<Pf2eLookups.Pf2eFlatModifier> _selectedFeatModifiers = [];
    private List<Pf2eLookups.Pf2eFlatModifier> _selectedItemModifiers = [];
    private List<Pf2eLookups.EquippedItemContext> _equippedItemContexts = [];
    private Pf2eLookups.AncestryRollContext _ancestryRollContext = new(null, [], null);
    private List<string> _terrainTags = [];
    private string _ambientLighting = "bright";
    private string _selectedTerrainTag = "";
    private readonly Dictionary<string, List<Pf2eLookups.Pf2eConditionModifier>> _conditionModsBySlug = [];
    private readonly Dictionary<Guid, List<string>> _monsterTraitsCache = [];

    private static string FormatBonus(int bonus) => bonus >= 0 ? $"+{bonus}" : bonus.ToString();

    private int CurrentStrikeIndex(Guid tokenId) => _strikesThisTurn.GetValueOrDefault(tokenId, 0);

    private void IncrementStrike(Guid tokenId) => _strikesThisTurn[tokenId] = CurrentStrikeIndex(tokenId) + 1;

    private int CharacterAttackBonus(Pf2eLookups.Pf2eAttack attack, Guid tokenId, bool agile = false)
    {
        var map = _state?.CombatActive == true ? Pf2eLookups.MapPenalty(CurrentStrikeIndex(tokenId), agile) : 0;
        return Pf2eLookups.Bonus(attack.Rank, _selectedCharacterLevel)
               + _selectedCharacterAbilityMods.GetValueOrDefault(attack.AbilityKey)
               + CheckModifier("attack") + map;
    }

    private int MonsterAttackBonus(Pf2eLookups.Pf2eMonsterAttack attack, Guid tokenId)
    {
        var map = _state?.CombatActive == true ? Pf2eLookups.MapPenalty(CurrentStrikeIndex(tokenId)) : 0;
        return attack.Bonus + map;
    }

    private int? TargetArmorClass => TargetToken?.ArmorClass;

    private TableTokenDto? SelectedToken => _tokens.FirstOrDefault(t => t.Id == _selectedTokenId);
    private TableTokenDto? TargetToken => _tokens.FirstOrDefault(t => t.Id == _targetTokenId);

    private async Task LoadCombatModifierDataAsync()
    {
        _selectedFeatModifiers = [];
        _selectedItemModifiers = [];
        _equippedItemContexts = [];
        _ancestryRollContext = Pf2eLookups.BuildAncestryRollContext(_selectedCharacterRace);
        var ancestrySlug = _ancestryRollContext.Slug;
        if (ancestrySlug is not null)
        {
            try
            {
                var raceEntries = await Api.GetRuleEntriesBySlugsAsync("pf2e", "race", new BatchSlugsRequest([ancestrySlug]));
                var raceStats = raceEntries.FirstOrDefault()?.StatsJson;
                if (raceStats is not null)
                    _ancestryRollContext = Pf2eLookups.BuildAncestryRollContext(_selectedCharacterRace, raceStats);
            }
            catch { /* hardcoded fallback в BuildAncestryRollContext */ }
        }

        var slugs = _selectedCharacterStats?.Feats
            .Where(f => f.Slug is not null).Select(f => f.Slug!).Distinct().ToList() ?? [];
        if (slugs.Count > 0)
        {
            try
            {
                var entries = await Api.GetRuleEntriesBySlugsAsync("pf2e", "feat", new BatchSlugsRequest(slugs));
                _selectedFeatModifiers = entries.SelectMany(e => Pf2eLookups.ParseFeatModifiers(e.StatsJson)).ToList();
            }
            catch { /* без модификаторов фитов на этот рендер */ }
        }

        var equippedSlugs = _selectedCharacterStats?.Inventory
            .Where(i => i.Equipped)
            .Select(i => i.Slug ?? Pf2eLookups.SlugifyItemName(i.Name))
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        if (equippedSlugs.Count > 0)
        {
            try
            {
                var entries = await Api.GetRuleEntriesBySlugsAsync("pf2e", "equipment", new BatchSlugsRequest(equippedSlugs));
                _equippedItemContexts = entries.Select(e => Pf2eLookups.ParseEquipmentContext(e.Slug, e.StatsJson)).ToList();
                _selectedItemModifiers = entries.SelectMany(e => Pf2eLookups.ParseItemModifiers(e.StatsJson)).ToList();
            }
            catch { /* без модификаторов снаряжения на этот рендер */ }
        }

        await EnsureConditionStatsAsync();
    }

    // Статы состояний (формулы штрафов) подгружаются батчем по слагам, которые сейчас висят на
    // жетонах, и кешируются на всё время за столом — состояний всего 43, лишних запросов нет.
    private async Task EnsureConditionStatsAsync()
    {
        var missing = _tokens.SelectMany(t => t.Conditions.Select(c => c.Slug))
            .Distinct().Where(s => !_conditionModsBySlug.ContainsKey(s)).ToList();
        if (missing.Count == 0) return;

        try
        {
            var entries = await Api.GetRuleEntriesBySlugsAsync("pf2e", "condition", new BatchSlugsRequest(missing));
            foreach (var entry in entries)
                _conditionModsBySlug[entry.Slug] = Pf2eLookups.ParseConditionModifiers(entry.StatsJson);
            foreach (var slug in missing.Where(s => !_conditionModsBySlug.ContainsKey(s)))
                _conditionModsBySlug[slug] = [];
        }
        catch { /* повторим при следующем выборе жетона */ }
    }

    private async Task SetTargetAsync(Guid? tokenId)
    {
        _targetTokenId = tokenId;
        var target = TargetToken;
        if (target is { CombatantType: "Pf2eMonster", CombatantId: { } monsterId }
            && !_monsterTraitsCache.ContainsKey(monsterId))
        {
            try
            {
                var monster = await Api.GetPf2eMonsterAsync(monsterId);
                _monsterTraitsCache[monsterId] = monster.Traits
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.ToLowerInvariant()).ToList();
            }
            catch { _monsterTraitsCache[monsterId] = []; }
        }

        await EnsureConditionStatsAsync();
        StateHasChanged();
    }

    private HashSet<string> BuildCombatRollOptions()
    {
        var options = _selectedCharacterStats?.Feats
            .Where(f => f.Slug is not null).Select(f => $"feat:{f.Slug}").ToHashSet() ?? [];

        if (_state?.CombatActive == true) options.Add("encounter");
        if (!string.IsNullOrEmpty(_selectedActionSlug)) options.Add($"action:{_selectedActionSlug}");

        if (SelectedToken is { } self)
            foreach (var c in self.Conditions)
                options.Add($"self:condition:{c.Slug}");

        if (TargetToken is { } target)
        {
            foreach (var c in target.Conditions)
                options.Add($"target:condition:{c.Slug}");
            if (target is { CombatantType: "Pf2eMonster", CombatantId: { } monsterId }
                && _monsterTraitsCache.TryGetValue(monsterId, out var traits))
                foreach (var trait in traits)
                    options.Add($"target:trait:{trait}");
        }

        Pf2eLookups.AddEquippedItemRollOptions(options, _selectedCharacterStats?.Inventory ?? [], _equippedItemContexts);
        Pf2eLookups.AddAncestryRollOptions(options, _ancestryRollContext);
        Pf2eLookups.AddSceneEnvironmentRollOptions(options, _terrainTags, _ambientLighting);

        return options;
    }

    private Dictionary<string, double> BuildCombatFacts()
    {
        var facts = new Dictionary<string, double> { ["self:level"] = _selectedCharacterLevel };
        if (SelectedToken is { CurrentHp: { } hp, MaxHp: > 0 and var maxHp })
            facts["hp-percent"] = 100.0 * hp / maxHp;
        if (SelectedToken is { } self)
            foreach (var c in self.Conditions.Where(c => c.Value is not null))
                facts[$"self:condition:{c.Slug}:value"] = c.Value!.Value;
        foreach (var (skill, rank) in _selectedCharacterStats?.SkillRanks ?? [])
            facts[$"skill:{skill}:rank"] = rank switch { >= 8 => 4, >= 6 => 3, >= 4 => 2, >= 2 => 1, _ => 0 };
        return facts;
    }

    // Итоговый контекстный модификатор проверки: фиты (предикаты против боевых roll options)
    // + состояния своего жетона (селектор "all" применяется к любой проверке), со стекингом
    // по типам PF2e (status/item/circumstance не складываются между собой).
    private int CheckModifier(string selector)
    {
        var options = BuildCombatRollOptions();
        var facts = BuildCombatFacts();
        var parts = new List<(string Type, int Value)>();

        foreach (var mod in _selectedFeatModifiers)
        {
            if (mod.Selector != selector && mod.Selector != "all") continue;
            if (!Pf2eLookups.PredicateEvaluator.Evaluate(mod.Predicate, options, facts)) continue;
            parts.Add((mod.Type, mod.Value));
        }

        foreach (var mod in _selectedItemModifiers)
        {
            if (mod.Selector != selector && mod.Selector != "all") continue;
            if (!Pf2eLookups.PredicateEvaluator.Evaluate(mod.Predicate, options, facts)) continue;
            parts.Add((mod.Type, mod.Value));
        }

        if (SelectedToken is { } self)
        {
            foreach (var c in self.Conditions)
            {
                if (!_conditionModsBySlug.TryGetValue(c.Slug, out var mods)) continue;
                foreach (var mod in mods)
                {
                    if (!mod.Selectors.Contains(selector) && !mod.Selectors.Contains("all")) continue;
                    var value = mod.Kind == "per_badge" ? mod.Amount * (c.Value ?? 1) : mod.Amount;
                    parts.Add((mod.Type, value));
                }
            }
        }

        return Pf2eLookups.StackModifiers(parts);
    }

    // Автоподстановка бонуса из PF2e-листа персонажа — не нужно вводить DC/бонус руками,
    // как в обычной форме «Проверка PF2e» ниже (та осталась для монстров и D&D5e-столов).
    private async Task RollPf2eSkillAsync(string key, string label, int rank)
    {
        var bonus = Pf2eLookups.Bonus(rank, _selectedCharacterLevel) + CheckModifier(key);
        var expression = bonus >= 0 ? $"1d20+{bonus}" : $"1d20{bonus}";
        _rolling = true;
        try { await Api.RollTableDiceAsync(Id, new RollDiceRequest(expression, null, label)); }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    private async Task RollPf2eAttackAsync(Pf2eLookups.Pf2eAttack attack, bool agile = false)
    {
        if (SelectedToken is not { } token) return;

        var bonus = CharacterAttackBonus(attack, token.Id, agile);
        var expression = bonus >= 0 ? $"1d20+{bonus}" : $"1d20{bonus}";
        var mapNote = _state?.CombatActive == true && CurrentStrikeIndex(token.Id) > 0
            ? $" (MAP {FormatBonus(Pf2eLookups.MapPenalty(CurrentStrikeIndex(token.Id), agile))})"
            : "";
        _rolling = true;
        try
        {
            await Api.RollTableDiceAsync(Id, new RollDiceRequest(
                expression, TargetArmorClass, $"{attack.Name} (атака){mapNote}"));
            if (_state?.CombatActive == true)
                IncrementStrike(token.Id);
        }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    private async Task RollPf2eDamageAsync(Pf2eLookups.Pf2eAttack attack)
    {
        var expression = attack.DamageBonus switch
        {
            > 0 => $"{attack.DamageDice}+{attack.DamageBonus}",
            < 0 => $"{attack.DamageDice}{attack.DamageBonus}",
            _ => attack.DamageDice
        };
        _rolling = true;
        try { await Api.RollTableDiceAsync(Id, new RollDiceRequest(expression, null, $"{attack.Name} (урон)")); }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    // L.2 — заклинания: атака и объявление DC из ранга спеллкаста на листе (без списания слотов).
    private int SelectedSpellAbilityMod =>
        _selectedCharacterStats is null ? 0 : _selectedCharacterAbilityMods.GetValueOrDefault(_selectedCharacterStats.KeyAbility);

    private int SelectedSpellAttackBonus(Guid tokenId)
    {
        if (_selectedCharacterStats is null) return 0;
        var baseBonus = Pf2eLookups.SpellAttackBonus(
            _selectedCharacterStats.SpellcastingRank, _selectedCharacterLevel, SelectedSpellAbilityMod);
        var map = _state?.CombatActive == true ? Pf2eLookups.MapPenalty(CurrentStrikeIndex(tokenId)) : 0;
        return baseBonus + map;
    }

    private int SelectedSpellDc =>
        _selectedCharacterStats is null ? 0
        : Pf2eLookups.SpellDc(_selectedCharacterStats.SpellcastingRank, _selectedCharacterLevel, SelectedSpellAbilityMod);

    private async Task RollPf2eSpellAttackAsync(Pf2eLookups.Pf2eKnownSpell spell)
    {
        if (SelectedToken is not { } token || _selectedCharacterStats is null) return;
        if (!await TryConsumeSpellSlotAsync(token, spell)) return;

        var bonus = SelectedSpellAttackBonus(token.Id);
        var expression = bonus >= 0 ? $"1d20+{bonus}" : $"1d20{bonus}";
        var mapNote = _state?.CombatActive == true && CurrentStrikeIndex(token.Id) > 0
            ? $" (MAP {FormatBonus(Pf2eLookups.MapPenalty(CurrentStrikeIndex(token.Id)))})"
            : "";
        _rolling = true;
        try
        {
            await Api.RollTableDiceAsync(Id, new RollDiceRequest(
                expression, TargetArmorClass, $"{spell.Name} (атака заклинанием){mapNote}"));
            if (_state?.CombatActive == true)
                IncrementStrike(token.Id);
        }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    private async Task AnnouncePf2eSpellDcAsync(Pf2eLookups.Pf2eKnownSpell spell)
    {
        if (SelectedToken is not { } token) return;
        if (!await TryConsumeSpellSlotAsync(token, spell)) return;

        try { await Api.SendTableChatAsync(Id, new SendChatRequest($"{spell.Name} — DC {SelectedSpellDc}")); }
        catch { _rollError = "Не удалось отправить сообщение."; }
    }

    // L.2 (продолжение) — списание слота при касте: заговоры (Level 0) не тратят слоты (at-will
    // по правилам PF2e), заклинания уровня 1+ списывают один слот соответствующего уровня. Обе
    // "боевые" кнопки (атака заклинанием / объявить DC) представляют сам акт каста — какая из них
    // используется для конкретного заклинания, зависит от типа эффекта (атакующее vs спасброски),
    // поэтому списание в обеих, а не в одной "общей кнопке каста" (её тут нет).
    private async Task<bool> TryConsumeSpellSlotAsync(TableTokenDto token, Pf2eLookups.Pf2eKnownSpell spell)
    {
        if (spell.Level <= 0 || _selectedCharacterStats is null || token.CombatantId is not Guid characterId)
            return true;

        var slot = _selectedCharacterStats.SpellSlots.GetValueOrDefault(spell.Level, new Pf2eLookups.Pf2eSpellSlotLevel(0, 0));
        if (slot.Used >= slot.Max)
        {
            _rollError = $"Нет свободных слотов {spell.Level}-го уровня.";
            return false;
        }

        var slots = new Dictionary<int, Pf2eLookups.Pf2eSpellSlotLevel>(_selectedCharacterStats.SpellSlots)
        {
            [spell.Level] = slot with { Used = slot.Used + 1 }
        };
        _selectedCharacterStats = _selectedCharacterStats with { SpellSlots = slots };
        StateHasChanged();

        try { await Api.UpdatePf2eStatsAsync(characterId, new UpdatePf2eStatsRequest(_selectedCharacterStats.ToJson())); }
        catch { /* локально слот уже списан — переживёт до следующей полной перезагрузки листа */ }

        return true;
    }

    // Бонус атаки монстра уже готовое число из статблока (см. Pf2eMonster.AttacksJson) — в
    // отличие от RollPf2eAttackAsync для персонажей, тут не считаем ранг+уровень+характеристику.
    private async Task RollMonsterAttackAsync(Pf2eLookups.Pf2eMonsterAttack attack)
    {
        if (SelectedToken is not { } token) return;

        var bonus = MonsterAttackBonus(attack, token.Id);
        var expression = bonus >= 0 ? $"1d20+{bonus}" : $"1d20{bonus}";
        var mapNote = _state?.CombatActive == true && CurrentStrikeIndex(token.Id) > 0
            ? $" (MAP {FormatBonus(Pf2eLookups.MapPenalty(CurrentStrikeIndex(token.Id)))})"
            : "";
        _rolling = true;
        try
        {
            await Api.RollTableDiceAsync(Id, new RollDiceRequest(
                expression, TargetArmorClass, $"{attack.Name} (атака){mapNote}"));
            if (_state?.CombatActive == true)
                IncrementStrike(token.Id);
        }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    private async Task RollMonsterDamageAsync(Pf2eLookups.Pf2eMonsterAttack attack)
    {
        var expression = attack.DamageBonus switch
        {
            > 0 => $"{attack.DamageDice}+{attack.DamageBonus}",
            < 0 => $"{attack.DamageDice}{attack.DamageBonus}",
            _ => attack.DamageDice
        };
        _rolling = true;
        try { await Api.RollTableDiceAsync(Id, new RollDiceRequest(expression, null, $"{attack.Name} (урон)")); }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    private async Task ApplyConditionAsync(TableTokenDto token)
    {
        if (string.IsNullOrWhiteSpace(_newConditionSlug)) return;

        var condition = _pf2eConditions.FirstOrDefault(c => c.Slug == _newConditionSlug);
        var name = condition is not null ? LocalizedConditionTitle(condition) : _newConditionSlug;

        try
        {
            await Api.ApplyTokenConditionAsync(Id, token.Id, new ApplyConditionRequest(_newConditionSlug, name, _newConditionValue));
            var idx = _tokens.FindIndex(t => t.Id == token.Id);
            if (idx >= 0)
            {
                var conditions = _tokens[idx].Conditions.Where(c => c.Slug != _newConditionSlug).ToList();
                conditions.Add(new TokenConditionDto(Guid.NewGuid(), _newConditionSlug, name, _newConditionValue));
                _tokens[idx] = _tokens[idx] with { Conditions = conditions };
            }
            _newConditionSlug = string.Empty;
            _newConditionValue = null;
        }
        catch { /* ignore */ }
    }

    private async Task RemoveConditionAsync(TableTokenDto token, string slug)
    {
        var idx = _tokens.FindIndex(t => t.Id == token.Id);
        if (idx >= 0)
            _tokens[idx] = _tokens[idx] with { Conditions = _tokens[idx].Conditions.Where(c => c.Slug != slug).ToList() };
        StateHasChanged();

        try { await Api.RemoveTokenConditionAsync(Id, token.Id, slug); }
        catch { /* ignore */ }
    }

    private async Task AdjustHpAsync(TableTokenDto token, int delta)
    {
        if (token.CurrentHp is not { } current) return;
        var newHp = current + delta;
        if (token.MaxHp is { } maxHp)
            newHp = Math.Clamp(newHp, 0, maxHp);
        else
            newHp = Math.Max(0, newHp);

        var idx = _tokens.FindIndex(t => t.Id == token.Id);
        if (idx >= 0) _tokens[idx] = token with { CurrentHp = newHp };
        _ = FlashHpChangeAsync(token.Id, delta);
        StateHasChanged();

        try
        {
            await Api.UpdateTableTokenStatsAsync(Id, token.Id, new UpdateTokenStatsRequest(newHp, null, null, null));
            if (newHp == 0 && idx >= 0)
                await TryApplyDyingAsync(_tokens[idx]);
        }
        catch { /* ignore */ }
    }

    // L.1 — при 0 HP в активном бою автоматически наложить Dying 1 (если ещё нет).
    private async Task TryApplyDyingAsync(TableTokenDto token)
    {
        if (_state?.CombatActive != true || token.CurrentHp is not 0) return;
        if (token.Conditions.Any(c => c.Slug == "dying")) return;

        const string slug = "dying";
        const string name = "Dying";

        try
        {
            await Api.ApplyTokenConditionAsync(Id, token.Id, new ApplyConditionRequest(slug, name, 1));
            var idx = _tokens.FindIndex(t => t.Id == token.Id);
            if (idx >= 0)
            {
                var conditions = _tokens[idx].Conditions.ToList();
                conditions.Add(new TokenConditionDto(Guid.NewGuid(), slug, name, 1));
                _tokens[idx] = _tokens[idx] with { Conditions = conditions };
                StateHasChanged();
            }
        }
        catch { /* ignore */ }
    }

    // Поворот на 45° за клик — как быстрые +5/-5 HP: без слайдера, но достаточно для боевых
    // ситуаций ("развернуть спиной", "смотрит на дверь").
    private async Task RotateTokenAsync(TableTokenDto token, int deltaDegrees)
    {
        var newRotation = ((token.Rotation + deltaDegrees) % 360 + 360) % 360;
        var idx = _tokens.FindIndex(t => t.Id == token.Id);
        if (idx >= 0) _tokens[idx] = token with { Rotation = newRotation };
        StateHasChanged();

        try { await Api.UpdateTableTokenStatsAsync(Id, token.Id, new UpdateTokenStatsRequest(null, null, null, newRotation)); }
        catch { /* ignore */ }
    }

    // J.2 — трекер инициативы: порядок хода вычисляется на клиенте так же, как на сервере
    // (InitiativeOrder.Sorted) — токены с заданной Initiative, по убыванию, стабильно по Id.
    // Дублирование логики сортировки небольшое (5 строк) и не стоит гонять токены через API
    // только чтобы узнать порядок для подсветки — сервер всё равно источник истины для
    // "чей сейчас ход" (CombatTurnTokenId), сортировка тут только для отображения списка.
    private List<TableTokenDto> InitiativeOrder =>
        _tokens.Where(t => t.Initiative is not null)
            .OrderByDescending(t => t.Initiative)
            .ThenBy(t => t.Id)
            .ToList();

    private async Task SetInitiativeAsync(TableTokenDto token, int? value)
    {
        var idx = _tokens.FindIndex(t => t.Id == token.Id);
        if (idx >= 0) _tokens[idx] = token with { Initiative = value };
        StateHasChanged();

        try { await Api.UpdateTableTokenStatsAsync(Id, token.Id, new UpdateTokenStatsRequest(null, null, null, null, SetInitiative: true, Initiative: value)); }
        catch { /* ignore */ }
    }

    // J.3 — тёмное зрение токена (доступно и владельцу, и ГМ, как инициатива/HP).
    private async Task ToggleDarkvisionAsync(TableTokenDto token)
    {
        var newValue = !token.HasDarkvision;
        var idx = _tokens.FindIndex(t => t.Id == token.Id);
        if (idx >= 0) _tokens[idx] = token with { HasDarkvision = newValue };
        StateHasChanged();

        try { await Api.UpdateTableTokenStatsAsync(Id, token.Id, new UpdateTokenStatsRequest(null, null, null, null, HasDarkvision: newValue)); }
        catch { /* ignore */ }
    }

    private async Task ToggleLowLightVisionAsync(TableTokenDto token)
    {
        var newValue = !token.HasLowLightVision;
        var idx = _tokens.FindIndex(t => t.Id == token.Id);
        if (idx >= 0) _tokens[idx] = token with { HasLowLightVision = newValue };
        StateHasChanged();

        try { await Api.UpdateTableTokenStatsAsync(Id, token.Id, new UpdateTokenStatsRequest(null, null, null, null, HasLowLightVision: newValue)); }
        catch { /* ignore */ }
    }

    // J.7 — переключатель "скрыт от игроков": пустой список VisibleToUserIds означает "виден
    // только ГМ". Повторное нажатие снимает ограничение целиком (снова видят все) — быстрый
    // способ спрятать/показать жетон-сюрприз, не отмечая игроков по одному.
    private async Task ToggleTokenHiddenAsync(TableTokenDto token)
    {
        var isHidden = token.VisibleToUserIds is not null && token.VisibleToUserIds.Count == 0;
        var newValue = isHidden ? null : new List<Guid>();
        await SetTokenVisibilityAsync(token, newValue);
    }

    // Точечно добавляет/убирает одного игрока из списка тех, кому виден жетон. Если жетон был
    // без ограничений (null = видят все), первое снятие галочки переводит его в режим "видят
    // все, кроме этого игрока" — список наполняется всеми участниками, кроме исключённого.
    private async Task ToggleTokenVisibleToAsync(TableTokenDto token, Guid playerUserId)
    {
        var players = _state?.Participants.Where(p => !p.IsDungeonMaster).Select(p => p.UserId).ToList() ?? [];
        var current = token.VisibleToUserIds ?? players;
        var newValue = current.Contains(playerUserId)
            ? current.Where(id => id != playerUserId).ToList()
            : [.. current, playerUserId];

        // Если после изменения список содержит вообще всех игроков — снимаем ограничение целиком.
        await SetTokenVisibilityAsync(token, players.All(newValue.Contains) ? null : newValue);
    }

    private async Task SetTokenVisibilityAsync(TableTokenDto token, List<Guid>? visibleToUserIds)
    {
        var idx = _tokens.FindIndex(t => t.Id == token.Id);
        if (idx >= 0) _tokens[idx] = token with { VisibleToUserIds = visibleToUserIds };
        StateHasChanged();

        try { await Api.SetTableTokenVisibilityAsync(Id, token.Id, new SetTokenVisibilityRequest(visibleToUserIds)); }
        catch { /* ignore */ }
    }

    private async Task StartCombatAsync()
    {
        try { await Api.StartTableCombatAsync(Id); }
        catch { /* ignore — состояние подтянется по SignalR или после перезагрузки */ }
    }

    private async Task EndCombatAsync()
    {
        try { await Api.EndTableCombatAsync(Id); }
        catch { /* ignore */ }
    }

    private async Task NextTurnAsync()
    {
        try { await Api.NextTableTurnAsync(Id); }
        catch { /* ignore */ }
    }

    private async Task PreviousTurnAsync()
    {
        try { await Api.PreviousTableTurnAsync(Id); }
        catch { /* ignore */ }
    }

    private async Task UploadTokenImageAsync(TableTokenDto token, InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file.Size > 5 * 1024 * 1024) return;

        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var part = new Refit.StreamPart(ms, file.Name, file.ContentType);
            var response = await Api.UploadTokenImageAsync(Id, token.Id, part);
            var idx = _tokens.FindIndex(t => t.Id == token.Id);
            if (idx >= 0) _tokens[idx] = _tokens[idx] with { ImageUrl = response.Url };
        }
        catch { /* ignore */ }
    }

    private async Task RemoveTokenAsync(Guid tokenId)
    {
        try { await Api.RemoveTableTokenAsync(Id, tokenId); }
        catch { /* ignore */ }
    }

    private async Task SetGridCellSizeAsync(int px)
    {
        _gridCellSizePx = px;
        try { await Api.SetTableGridCellSizeAsync(Id, new SetGridCellSizeRequest(px)); }
        catch { /* ignore */ }
    }

    // Туман войны — упрощённый радиус зрения вокруг своих токенов, не point-in-polygon видимость
    // со стенами (см. комментарий на GameSession.FogEnabled). GM всегда видит карту целиком —
    // оверлей тумана для него не рендерится вовсе (см. Table.razor).
    private async Task SetFogSettingsAsync(bool enabled, int radiusFeet)
    {
        _fogEnabled = enabled;
        _visionRadiusFeet = radiusFeet;
        try { await Api.SetTableFogSettingsAsync(Id, new SetFogSettingsRequest(enabled, radiusFeet)); }
        catch { /* ignore */ }
    }

    private async Task SetSceneEnvironmentAsync(string terrainTag, string ambientLighting)
    {
        _selectedTerrainTag = terrainTag;
        _terrainTags = string.IsNullOrWhiteSpace(terrainTag) ? [] : [terrainTag];
        _ambientLighting = ambientLighting;
        var terrainJson = _terrainTags.Count == 0 ? null : JsonSerializer.Serialize(_terrainTags);
        try { await Api.SetTableSceneEnvironmentAsync(Id, new SetSceneEnvironmentRequest(terrainJson, ambientLighting)); }
        catch { /* ignore */ }
    }

    private Task OnAmbientLightingChanged(ChangeEventArgs e) =>
        SetSceneEnvironmentAsync(_selectedTerrainTag, e.Value?.ToString() ?? "bright");


    // L.7 — общая линейка через SignalR (см. TableHub.BroadcastMeasure).
    private async Task ToggleMeasureModeAsync()
    {
        _measureMode = !_measureMode;
        if (_measureMode)
        {
            _pingMode = false;
            await DisableTemplateModeAsync();
        }
        try
        {
            await Js.InvokeVoidAsync("tableDrag.setMeasureMode", _measureMode);
            await Js.InvokeVoidAsync("tableDrag.setPingMode", false);
        }
        catch { /* ignore */ }
    }

    private async Task TogglePingModeAsync()
    {
        _pingMode = !_pingMode;
        if (_pingMode)
        {
            _measureMode = false;
            await DisableTemplateModeAsync();
        }
        try
        {
            await Js.InvokeVoidAsync("tableDrag.setPingMode", _pingMode);
            await Js.InvokeVoidAsync("tableDrag.setMeasureMode", false);
        }
        catch { /* ignore */ }
    }

    [JSInvokable]
    public async Task OnMeasureDrawn(double x1, double y1, double x2, double y2, int feet)
    {
        try { await Js.InvokeVoidAsync("tableDrag.showRemoteMeasure", x1, y1, x2, y2, feet); } catch { /* ignore */ }
        if (_hub is { State: HubConnectionState.Connected })
        {
            try { await _hub.SendAsync("BroadcastMeasure", Id.ToString(), x1, y1, x2, y2, feet); }
            catch { /* ignore */ }
        }
    }

    [JSInvokable]
    public async Task OnPingPlaced(double x, double y)
    {
        try { await Js.InvokeVoidAsync("tableDrag.showPing", x, y); } catch { /* ignore */ }
        if (_hub is { State: HubConnectionState.Connected })
        {
            try { await _hub.SendAsync("BroadcastPing", Id.ToString(), x, y); }
            catch { /* ignore */ }
        }
    }

    // Шаблон зоны поражения — тот же принцип, что и линейка: чисто визуальная прикидка на
    // клиенте, режимы взаимоисключающие (одновременно и линейка, и шаблон — путаница в кликах).
    private async Task ToggleTemplateModeAsync()
    {
        _templateMode = !_templateMode;
        if (_templateMode)
        {
            _measureMode = false;
            try { await Js.InvokeVoidAsync("tableDrag.setMeasureMode", false); } catch { /* ignore */ }
        }
        await ApplyTemplateModeAsync();
    }

    private async Task DisableTemplateModeAsync()
    {
        _templateMode = false;
        try { await Js.InvokeVoidAsync("tableDrag.setTemplateMode", null, 0); } catch { /* ignore */ }
    }

    private async Task ApplyTemplateModeAsync()
    {
        try
        {
            await Js.InvokeVoidAsync("tableDrag.setTemplateMode", _templateMode ? _templateType : null, _templateFeet);
        }
        catch { /* ignore */ }
    }

    // J.5 — вызывается из JS при отпускании кнопки мыши в режиме шаблона (уже снапнуто к сетке
    // на стороне JS). Считаем задетые токены и рассылаем остальным участникам напрямую через хаб
    // (шаблон эфемерный, не хранится в БД — см. TableHub.BroadcastTemplate).
    [JSInvokable]
    public async Task OnTemplatePlaced(string type, int feet, double originX, double originY, double angleDeg)
    {
        _placedTemplate = new PlacedTemplate(type, feet, originX, originY, angleDeg);
        RecomputeTemplateAffectedTokens();
        StateHasChanged();

        if (_hub is { State: HubConnectionState.Connected })
        {
            try { await _hub.SendAsync("BroadcastTemplate", Id.ToString(), type, feet, originX, originY, angleDeg); }
            catch { /* ignore */ }
        }
    }

    private async Task ClearTemplateAsync()
    {
        _placedTemplate = null;
        _templateAffectedTokenIds = [];
        try { await Js.InvokeVoidAsync("tableDrag.clearPlacedTemplate"); } catch { /* ignore */ }

        if (_hub is { State: HubConnectionState.Connected })
        {
            try { await _hub.SendAsync("ClearTemplate", Id.ToString()); }
            catch { /* ignore */ }
        }
    }

    // Геометрия в футах (1 клетка = 5 фт), упрощённая под ту же модель, что и визуальный оверлей
    // в token-drag.js: сфера — круг радиусом feet, линия — прямоугольник шириной 5 фт по длине
    // feet, конус — сектор ~90° (половинный угол 45°) длиной feet. Токен считается задетым, если
    // его центр (X+Width/2, Y+Height/2 в клетках) попадает в фигуру.
    private void RecomputeTemplateAffectedTokens()
    {
        _templateAffectedTokenIds = [];
        if (_placedTemplate is not { } t) return;

        foreach (var token in _tokens)
        {
            var tokenX = (token.X + token.Width / 2.0 - t.OriginX) * 5;
            var tokenY = (token.Y + token.Height / 2.0 - t.OriginY) * 5;
            var distance = Math.Sqrt(tokenX * tokenX + tokenY * tokenY);

            var hit = t.Type switch
            {
                "burst" => distance <= t.Feet,
                "line" => IsWithinLine(tokenX, tokenY, t.AngleDeg, t.Feet),
                "cone" => distance <= t.Feet && IsWithinConeAngle(tokenX, tokenY, t.AngleDeg),
                _ => false,
            };

            if (hit) _templateAffectedTokenIds.Add(token.Id);
        }
    }

    private static bool IsWithinLine(double x, double y, double angleDeg, int feet)
    {
        var angleRad = angleDeg * Math.PI / 180;
        var along = x * Math.Cos(angleRad) + y * Math.Sin(angleRad);
        var across = -x * Math.Sin(angleRad) + y * Math.Cos(angleRad);
        return along >= 0 && along <= feet && Math.Abs(across) <= 2.5;
    }

    private static bool IsWithinConeAngle(double x, double y, double angleDeg)
    {
        if (x == 0 && y == 0) return true;
        var toToken = Math.Atan2(y, x) * 180 / Math.PI;
        var diff = Math.Abs(((toToken - angleDeg) + 540) % 360 - 180);
        return diff <= 45;
    }

    // Режим рисования стен — взаимоисключающий с линейкой/шаблоном по той же причине (клики
    // не должны означать одновременно "нарисовать стену" и "измерить дистанцию").
    private async Task ToggleWallModeAsync()
    {
        _wallMode = !_wallMode;
        if (_wallMode)
        {
            _measureMode = false;
            _templateMode = false;
            try { await Js.InvokeVoidAsync("tableDrag.setMeasureMode", false); } catch { /* ignore */ }
            try { await Js.InvokeVoidAsync("tableDrag.setTemplateMode", null, 0); } catch { /* ignore */ }
        }
        try { await Js.InvokeVoidAsync("tableDrag.setWallMode", _wallMode); } catch { /* ignore */ }
    }

    private async Task ClearWallsAsync()
    {
        _walls = [];
        try { await Api.SetTableWallsAsync(Id, new SetWallsRequest(null)); }
        catch { /* ignore */ }
    }

    // J.3 — режим размещения источника света, взаимоисключающий с остальными режимами клика по
    // сцене (стены/линейка/шаблон), по той же причине.
    private async Task ToggleLightModeAsync()
    {
        _lightMode = !_lightMode;
        if (_lightMode)
        {
            _measureMode = false;
            _templateMode = false;
            _wallMode = false;
            try { await Js.InvokeVoidAsync("tableDrag.setMeasureMode", false); } catch { /* ignore */ }
            try { await Js.InvokeVoidAsync("tableDrag.setTemplateMode", null, 0); } catch { /* ignore */ }
            try { await Js.InvokeVoidAsync("tableDrag.setWallMode", false); } catch { /* ignore */ }
        }
        try { await Js.InvokeVoidAsync("tableDrag.setLightMode", _lightMode); } catch { /* ignore */ }
    }

    [JSInvokable]
    public async Task OnLightPlaced(double x, double y)
    {
        _lights = [.. _lights, new LightDto(x, y, 20, 20, LightColors[0])];
        StateHasChanged();
        await SaveLightsAsync();
    }

    private async Task UpdateLightAsync(int index, LightDto light)
    {
        var list = _lights.ToList();
        list[index] = light;
        _lights = list;
        await SaveLightsAsync();
    }

    private async Task RemoveLightAsync(int index)
    {
        _lights = [.. _lights.Where((_, i) => i != index)];
        await SaveLightsAsync();
    }

    private async Task SaveLightsAsync()
    {
        try { await Api.SetTableLightsAsync(Id, new SetLightsRequest(_lights.Count > 0 ? JsonSerializer.Serialize(_lights, WallsJsonOptions) : null)); }
        catch { /* ignore */ }
    }

    // J.4 — множественные сцены. Переключение сцены (как и создание/переименование/удаление)
    // рассылается всем через SignalR ("ActiveSceneChanged"), включая инициатора — не обновляем
    // локальное состояние оптимистично здесь, ждём общий путь LoadTableStateAsync через хаб,
    // чтобы не было двух источников истины при гонке с чужими правками.
    private async Task CreateSceneAsync()
    {
        if (string.IsNullOrWhiteSpace(_newSceneName)) return;
        try { await Api.CreateSceneAsync(Id, new CreateSceneRequest(_newSceneName.Trim())); _newSceneName = string.Empty; }
        catch { /* ignore */ }
    }

    private async Task RenameSceneAsync(Guid sceneId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        try { await Api.RenameSceneAsync(Id, sceneId, new CreateSceneRequest(name.Trim())); }
        catch { /* ignore */ }
    }

    private string? _sceneError;

    private async Task DeleteSceneAsync(Guid sceneId)
    {
        _sceneError = null;
        try { await Api.DeleteSceneAsync(Id, sceneId); }
        catch (Exception ex) { _sceneError = ex.Message.Contains("LastOne") ? "Нельзя удалить последнюю сцену." : "Не удалось удалить сцену."; }
    }

    private async Task SwitchSceneAsync(Guid sceneId)
    {
        if (_state?.ActiveSceneId == sceneId) return;
        try { await Api.ActivateSceneAsync(Id, sceneId); }
        catch { /* ignore */ }
    }

    // Журнал мастера — упрощённая версия Foundry Journal: одна общая видимость на запись
    // ("опубликовано игрокам да/нет"), не список конкретных получателей на каждую запись.
    private void StartNewJournalEntry()
    {
        _editingJournalEntryId = Guid.Empty;
        _journalTitleInput = string.Empty;
        _journalContentInput = string.Empty;
        _journalParentId = null;
        _journalCampaignId = null;
        _journalError = null;
    }

    private void StartNewJournalChildEntry(Guid parentId)
    {
        _editingJournalEntryId = Guid.Empty;
        _journalTitleInput = string.Empty;
        _journalContentInput = string.Empty;
        _journalParentId = parentId;
        _journalCampaignId = null;
        _journalError = null;
    }

    private void StartEditJournalEntry(JournalEntryDto entry)
    {
        _editingJournalEntryId = entry.Id;
        _journalTitleInput = entry.Title;
        _journalContentInput = entry.ContentMarkdown;
        _journalParentId = entry.ParentId;
        _journalCampaignId = entry.CampaignId;
        _journalError = null;
    }

    private void CancelJournalEdit()
    {
        _editingJournalEntryId = null;
        _journalError = null;
    }

    private async Task SaveJournalEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(_journalTitleInput)) return;

        try
        {
            if (_editingJournalEntryId == Guid.Empty)
            {
                var created = await Api.CreateJournalEntryAsync(Id, new CreateJournalEntryRequest(
                    _journalTitleInput, _journalContentInput, _journalParentId, _journalCampaignId));
                _journalEntries.Insert(0, created);
            }
            else if (_editingJournalEntryId is { } entryId)
            {
                await Api.UpdateJournalEntryAsync(Id, entryId, new CreateJournalEntryRequest(
                    _journalTitleInput, _journalContentInput, _journalParentId, _journalCampaignId));
                var idx = _journalEntries.FindIndex(e => e.Id == entryId);
                if (idx >= 0)
                    _journalEntries[idx] = _journalEntries[idx] with
                    {
                        Title = _journalTitleInput,
                        ContentMarkdown = _journalContentInput,
                        ParentId = _journalParentId,
                        CampaignId = _journalCampaignId
                    };
            }
            _editingJournalEntryId = null;
        }
        catch { _journalError = "Не удалось сохранить запись."; }
    }

    private async Task TogglePublishJournalEntryAsync(JournalEntryDto entry)
    {
        var newState = !entry.IsPublished;
        try
        {
            await Api.SetJournalEntryPublishedAsync(Id, entry.Id, new SetJournalEntryPublishedRequest(newState));
            var idx = _journalEntries.FindIndex(e => e.Id == entry.Id);
            if (idx >= 0) _journalEntries[idx] = entry with { IsPublished = newState };
        }
        catch { /* ignore */ }
    }

    private async Task DeleteJournalEntryAsync(Guid entryId)
    {
        _journalEntries.RemoveAll(e => e.Id == entryId);
        StateHasChanged();
        try { await Api.DeleteJournalEntryAsync(Id, entryId); }
        catch { /* ignore */ }
    }

    [JSInvokable]
    public async Task OnTokenDragEnd(string tokenId, double x, double y)
    {
        if (!Guid.TryParse(tokenId, out var id)) return;

        var idx = _tokens.FindIndex(t => t.Id == id);
        if (idx >= 0) _tokens[idx] = _tokens[idx] with { X = x, Y = y };
        if (_placedTemplate is not null) RecomputeTemplateAffectedTokens();
        RecomputeFlanking();
        StateHasChanged();

        try { await Api.MoveTableTokenAsync(Id, id, new TokenPositionRequest(x, y)); }
        catch { /* ignore */ }
    }

    [JSInvokable]
    public async Task OnWallDrawn(double x1, double y1, double x2, double y2)
    {
        _walls = [.. _walls, new WallDto(x1, y1, x2, y2, _doorDrawMode, IsOpen: false)];
        StateHasChanged();

        try { await Api.SetTableWallsAsync(Id, new SetWallsRequest(JsonSerializer.Serialize(_walls, WallsJsonOptions))); }
        catch { /* ignore */ }
    }

    private async Task ToggleDoorAsync(int wallIndex)
    {
        if (wallIndex < 0 || wallIndex >= _walls.Count) return;
        var w = _walls[wallIndex];
        if (!w.IsDoor) return;
        _walls[wallIndex] = w with { IsOpen = !w.IsOpen };
        StateHasChanged();
        try { await Api.SetTableWallsAsync(Id, new SetWallsRequest(JsonSerializer.Serialize(_walls, WallsJsonOptions))); }
        catch { /* ignore */ }
    }

    private void RecomputeFlanking()
    {
        _flankedTokenIds = [];
        if (_state?.CombatActive != true) return;

        for (var i = 0; i < _tokens.Count; i++)
        {
            var target = _tokens[i];
            for (var a = 0; a < _tokens.Count; a++)
            for (var b = a + 1; b < _tokens.Count; b++)
            {
                if (a == i || b == i) continue;
                var ta = _tokens[a];
                var tb = _tokens[b];
                if (ta.OwnerId == target.OwnerId || tb.OwnerId == target.OwnerId) continue;
                if (ta.OwnerId is null && tb.OwnerId is null) continue;
                if (GridFlanking.IsFlanked(
                        ta.X, ta.Y, ta.Width, ta.Height,
                        tb.X, tb.Y, tb.Width, tb.Height,
                        target.X, target.Y, target.Width, target.Height))
                    _flankedTokenIds.Add(target.Id);
            }
        }
    }

    private bool IsFlanked(Guid tokenId) => _flankedTokenIds.Contains(tokenId);

    private async Task RollPf2eSaveAsync(string key, string label, int rank)
    {
        var bonus = Pf2eLookups.Bonus(rank, _selectedCharacterLevel) + CheckModifier(key);
        var expression = bonus >= 0 ? $"1d20+{bonus}" : $"1d20{bonus}";
        _rolling = true;
        try { await Api.RollTableDiceAsync(Id, new RollDiceRequest(expression, _saveTargetDc, label)); }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    private async Task RollMonsterSaveAsync(string label, int bonus)
    {
        var expression = bonus >= 0 ? $"1d20+{bonus}" : $"1d20{bonus}";
        var dc = _selectedCharacterStats is not null ? SelectedSpellDc : _checkDc;
        _rolling = true;
        try { await Api.RollTableDiceAsync(Id, new RollDiceRequest(expression, dc, label)); }
        catch { _rollError = "Не удалось выполнить бросок."; }
        finally { _rolling = false; }
    }

    private bool IsJournalVisibleToMe(JournalEntryDto entry)
    {
        if (_state?.IsOrganizer == true) return true;
        if (!entry.IsPublished) return false;
        if (_currentUserId is null) return false;
        return entry.VisibleToUserIds is null || entry.VisibleToUserIds.Contains(_currentUserId.Value);
    }

    private IEnumerable<JournalEntryDto> JournalRoots =>
        _journalEntries.Where(e => e.ParentId is null).OrderByDescending(e => e.UpdatedAt);

    private IEnumerable<JournalEntryDto> JournalChildren(Guid parentId) =>
        _journalEntries.Where(e => e.ParentId == parentId).OrderByDescending(e => e.UpdatedAt);

    private IEnumerable<(JournalEntryDto Entry, int Depth)> JournalFlatList()
    {
        foreach (var root in JournalRoots)
        {
            yield return (root, 0);
            foreach (var nested in FlattenJournalChildren(root.Id, 1))
                yield return nested;
        }
    }

    private IEnumerable<(JournalEntryDto Entry, int Depth)> FlattenJournalChildren(Guid parentId, int depth)
    {
        foreach (var child in JournalChildren(parentId))
        {
            yield return (child, depth);
            foreach (var nested in FlattenJournalChildren(child.Id, depth + 1))
                yield return nested;
        }
    }

    private async Task ToggleJournalPlayerVisibilityAsync(JournalEntryDto entry, Guid playerId)
    {
        var players = _state?.Participants.Where(p => !p.IsDungeonMaster).Select(p => p.UserId).ToList() ?? [];
        var current = entry.VisibleToUserIds ?? players;
        var next = current.Contains(playerId)
            ? current.Where(id => id != playerId).ToList()
            : [.. current, playerId];
        var visibleTo = next.Count == players.Count ? null : next;
        try
        {
            await Api.SetJournalEntryVisibilityAsync(Id, entry.Id, new SetJournalEntryVisibilityRequest(visibleTo));
            var idx = _journalEntries.FindIndex(e => e.Id == entry.Id);
            if (idx >= 0) _journalEntries[idx] = entry with { VisibleToUserIds = visibleTo };
        }
        catch { /* ignore */ }
    }

    private async Task ScrollChatToBottomAsync()
    {
        try { await Js.InvokeVoidAsync("tableHelpers.scrollToBottom", _chatLogRef); }
        catch { /* ignore */ }
    }

    private async Task BuildConditionTitlesAsync()
    {
        _conditionTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in _pf2eConditions)
            _conditionTitles[c.Slug] = await Locale.NameAsync("condition", c.Slug, c.Title);
    }

    private string LocalizedConditionTitle(RuleEntrySummaryDto condition) =>
        _conditionTitles.GetValueOrDefault(condition.Slug, condition.Title);

    private string LocalizedConditionName(string slug, string fallback) =>
        _conditionTitles.GetValueOrDefault(slug, fallback);

    private async void OnContentLanguageChanged()
    {
        await InvokeAsync(async () =>
        {
            await BuildConditionTitlesAsync();
            if (_monsterResults.Count > 0)
                await LocalizeMonsterResultsAsync();
            StateHasChanged();
        });
    }

    public async ValueTask DisposeAsync()
    {
        Lang.OnChanged -= OnContentLanguageChanged;
        if (_hub is not null)
        {
            if (_hub.State == HubConnectionState.Connected)
                await _hub.InvokeAsync("LeaveTable", Id.ToString());
            await _hub.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }
}
