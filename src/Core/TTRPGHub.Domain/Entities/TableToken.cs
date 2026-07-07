namespace TTRPGHub.Entities;

public sealed class TableToken
{
    public Guid Id { get; private init; }
    public GameSessionId SessionId { get; private init; }

    // J.4 — какой сцене (карте) в рамках сессии принадлежит токен; SessionId остался для
    // SignalR-группы/выборок "все токены сессии", а видимость/перемещение на конкретной карте
    // всегда фильтруется по SceneId (только токены активной сцены показываются участникам).
    public Guid SceneId { get; private init; }
    public string Label { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public string Color { get; private set; } = null!;
    public double X { get; private set; }
    public double Y { get; private set; }
    public int Width { get; private set; } = 1;
    public int Height { get; private set; } = 1;
    public int Rotation { get; private set; }
    public UserId? OwnerId { get; private init; }

    // Привязка токена к источнику статов (персонаж игрока или монстр из справочника) —
    // HP/AC копируются на токен при создании, дальше живут независимо (GM может править
    // прямо во время боя, не трогая исходного персонажа/монстра), как в Foundry.
    public TokenCombatantType CombatantType { get; private init; } = TokenCombatantType.None;
    public Guid? CombatantId { get; private init; }
    public int? CurrentHp { get; private set; }
    public int? MaxHp { get; private set; }
    public int? ArmorClass { get; private set; }

    // J.2 — трекер инициативы: значение броска инициативы для этого токена в текущем бою.
    // Null означает "не участвует в инициативе" (декорация, ещё не вступил в бой и т.п.) —
    // такие токены не показываются в трекере и пропускаются при переходе хода.
    public int? Initiative { get; private set; }

    // J.3 — упрощение вместо полноценной модели чувств PF2e (тёмное зрение/светочувствительность/
    // низкоуровневое зрение): один флаг "видит в темноте без источника света" покрывает
    // практический случай (тёмное зрение), а низкоуровневое зрение (видит тускло освещённое как
    // яркое) — не моделируем отдельно, слишком тонкое различие для первой версии освещения.
    public bool HasDarkvision { get; private set; }

    // L.7 — низкоуровневое зрение отдельно от тёмного: в тусклом освещении сцены видит без
    // маски источников света (как в PF2e dim light → bright для low-light).
    public bool HasLowLightVision { get; private set; }

    // J.7 — видимость токена конкретным игрокам (например, скрытая ловушка-монстр или NPC,
    // о котором должен знать только один игрок). null = виден всем участникам стола (поведение
    // по умолчанию, обратно совместимо с токенами до этого поля). Непустой список — виден только
    // перечисленным игрокам + GM + владельцу токена; пустой список ([]) — скрыт от всех игроков,
    // виден только GM. Храним как JSON-массив Guid, а не отдельную таблицу — список маленький и
    // меняется только руками GM, не нужен отдельный запрос/джойн.
    public string? VisibleToJson { get; private set; }

    // Состояния, наложенные прямо в бою (Frightened 2, Prone и т.д.) — живут на токене, а не
    // на исходном Character/Monster: состояние существует только в рамках текущей сцены/сессии,
    // после боя токен обычно удаляется, а персонаж остаётся "чистым".
    private readonly List<TokenCondition> _conditions = [];
    public IReadOnlyList<TokenCondition> Conditions => _conditions.AsReadOnly();

    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private TableToken() { }

    public static TableToken Create(
        GameSessionId sessionId, Guid sceneId, string label, string? imageUrl, string color,
        double x, double y, UserId? ownerId,
        int width = 1, int height = 1,
        TokenCombatantType combatantType = TokenCombatantType.None, Guid? combatantId = null,
        int? currentHp = null, int? maxHp = null, int? armorClass = null)
    {
        var now = DateTime.UtcNow;
        return new TableToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SceneId = sceneId,
            Label = label,
            ImageUrl = imageUrl,
            Color = color,
            X = ClampCoord(x),
            Y = ClampCoord(y),
            Width = ClampSize(width),
            Height = ClampSize(height),
            OwnerId = ownerId,
            CombatantType = combatantType,
            CombatantId = combatantId,
            CurrentHp = currentHp,
            MaxHp = maxHp,
            ArmorClass = armorClass,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool CanBeMovedBy(UserId userId, bool isOrganizer) =>
        isOrganizer || OwnerId == userId;

    // J.7 — парсинг JSON здесь (не в Application) через System.Text.Json нежелателен для Domain
    // (см. паттерн ResistancesJson/WeaknessesJson у Pf2eMonster — тот же JSON-на-сущности хранится
    // сырым, парсинг делает вызывающий слой), поэтому список игроков передаётся уже распарсенным.
    public bool IsVisibleTo(UserId userId, bool isOrganizer, IReadOnlyCollection<Guid>? visibleToUserIds) =>
        isOrganizer || OwnerId == userId || visibleToUserIds is null || visibleToUserIds.Contains(userId.Value);

    public void SetVisibility(string? visibleToJson)
    {
        VisibleToJson = visibleToJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Move(double x, double y)
    {
        X = ClampCoord(x);
        Y = ClampCoord(y);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resize(int width, int height)
    {
        Width = ClampSize(width);
        Height = ClampSize(height);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rotate(int degrees)
    {
        // Нормализуем в [0, 360) — отрицательные и "перекрученные" значения с клиента
        // (например, после нескольких кликов ротации) не должны накапливаться в мусор.
        Rotation = ((degrees % 360) + 360) % 360;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImage(string? imageUrl)
    {
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetInitiative(int? value)
    {
        Initiative = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDarkvision(bool value)
    {
        HasDarkvision = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLowLightVision(bool value)
    {
        HasLowLightVision = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateHp(int currentHp)
    {
        CurrentHp = MaxHp.HasValue ? Math.Clamp(currentHp, 0, MaxHp.Value) : Math.Max(0, currentHp);
        UpdatedAt = DateTime.UtcNow;
    }

    // Обратная синхронизация от привязанного персонажа (см. Character.SetCurrentHitPoints):
    // правки на листе персонажа (level-up, лечение вне боя) должны отражаться на его токене,
    // если тот сейчас стоит на какой-то карте — иначе GM видит устаревшие HP/AC в бою.
    public void SyncFromCharacter(int currentHp, int maxHp, int armorClass)
    {
        MaxHp = maxHp;
        CurrentHp = Math.Clamp(currentHp, 0, maxHp);
        ArmorClass = armorClass;
        UpdatedAt = DateTime.UtcNow;
    }

    // Повторное наложение того же состояния обновляет значение (стакающиеся состояния PF2e вроде
    // Frightened хранят одно текущее значение, не список — как в правилах: "уровень" состояния,
    // не отдельные наложения).
    public void ApplyCondition(string slug, string name, int? value)
    {
        var existing = _conditions.FirstOrDefault(c => c.Slug == slug);
        if (existing is not null)
            _conditions.Remove(existing);

        _conditions.Add(TokenCondition.Create(slug, name, value));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCondition(string slug)
    {
        _conditions.RemoveAll(c => c.Slug == slug);
        UpdatedAt = DateTime.UtcNow;
    }

    // Клетки сетки — не нормализованная доля [0,1] (как было раньше), а координата в клетках,
    // может быть дробной во время перетаскивания и округляется до целой на клиенте при отпускании.
    private static double ClampCoord(double value) => Math.Clamp(value, 0d, 200d);
    private static int ClampSize(int value) => Math.Clamp(value, 1, 6);
}

public enum TokenCombatantType { None, Character, Pf2eMonster, Dnd5eMonster }

public sealed class TokenCondition
{
    public Guid Id { get; private init; }
    public string Slug { get; private init; } = null!;
    public string Name { get; private init; } = null!;
    public int? Value { get; private init; }
    public DateTime AppliedAt { get; private init; }

    private TokenCondition() { }

    public static TokenCondition Create(string slug, string name, int? value) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Name = name,
        Value = value,
        AppliedAt = DateTime.UtcNow
    };
}
