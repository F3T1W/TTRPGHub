namespace TTRPGHub.Entities;

// J.4 — множественные сцены на сессию: раньше все поля ниже (карта/сетка/туман/стены/свет/бой)
// жили прямо на GameSession, что не давало ГМ иметь больше одной карты в рамках игры. Вынесены
// в отдельную сущность-агрегат (как TableToken — со своим репозиторием, а не owned-коллекцией на
// GameSession), потому что мутируются часто (перетаскивание тумана/боя не должно требовать
// загрузки/сохранения всех сцен сессии разом). Авторизацию (только ГМ) проверяет обработчик
// команды в Application, сверяя currentUser с GameSession.OrganizerId — сама Scene не хранит
// организатора и не содержит проверок доступа.
public sealed class Scene
{
    public Guid Id { get; private init; }
    public GameSessionId SessionId { get; private init; }
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }

    public string? ShowcaseImageUrl { get; private set; }
    public int GridCellSizePx { get; private set; } = 50;

    public bool FogEnabled { get; private set; }
    public int VisionRadiusFeet { get; private set; } = 30;
    public string? WallsJson { get; private set; }
    public string? LightsJson { get; private set; }

    // L.4 — контекст предикатов terrain:* и lighting:* для rule engine за столом.
    public string? TerrainTagsJson { get; private set; }
    public string AmbientLighting { get; private set; } = "bright";

    public bool CombatActive { get; private set; }
    public int CombatRound { get; private set; }
    public Guid? CombatTurnTokenId { get; private set; }

    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private Scene() { }

    public static Scene Create(GameSessionId sessionId, string name, int sortOrder)
    {
        var now = DateTime.UtcNow;
        return new Scene
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = name,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Rename(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetShowcaseImage(string? imageUrl)
    {
        ShowcaseImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetGridCellSize(int px)
    {
        GridCellSizePx = px;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFogSettings(bool enabled, int visionRadiusFeet)
    {
        FogEnabled = enabled;
        VisionRadiusFeet = visionRadiusFeet;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWalls(string? wallsJson)
    {
        WallsJson = wallsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLights(string? lightsJson)
    {
        LightsJson = lightsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEnvironment(string? terrainTagsJson, string ambientLighting)
    {
        TerrainTagsJson = terrainTagsJson;
        AmbientLighting = string.IsNullOrWhiteSpace(ambientLighting) ? "bright" : ambientLighting;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartCombat()
    {
        CombatActive = true;
        CombatRound = 1;
        CombatTurnTokenId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EndCombat()
    {
        CombatActive = false;
        CombatRound = 0;
        CombatTurnTokenId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCombatTurn(Guid? tokenId, int round)
    {
        CombatTurnTokenId = tokenId;
        CombatRound = Math.Max(1, round);
        UpdatedAt = DateTime.UtcNow;
    }
}
