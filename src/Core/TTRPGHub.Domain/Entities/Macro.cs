namespace TTRPGHub.Entities;

// Личная библиотека макросов пользователя — аналог Foundry Macro Directory + hotbar, но не
// привязана к конкретной сессии (как и Foundry-макросы не привязаны к конкретному актёру —
// один и тот же макрос игрок использует за любым своим столом). Chat-макрос — просто текст в
// чат (в т.ч. команды формата "/r 1d20+5"). Script-макрос выполняется в песочнице (sandboxed
// iframe, см. Web/wwwroot/js/macro-sandbox.js) против нашего API-шима (game.roll/game.chat/...),
// а не настоящего Foundry API — импортированный из Foundry код почти наверняка потребует
// правки под наш API, но структура (имя/иконка/команда) переносится как есть.
public sealed class Macro
{
    public Guid Id { get; private init; }
    public UserId OwnerId { get; private init; }
    public string Name { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public MacroType Type { get; private set; }
    public string Command { get; private set; } = null!;
    public int HotbarSlot { get; private set; } = -1;
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private Macro() { }

    public static Macro Create(UserId ownerId, string name, string? imageUrl, MacroType type, string command)
    {
        var now = DateTime.UtcNow;
        return new Macro
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            ImageUrl = imageUrl,
            Type = type,
            Command = command,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string? imageUrl, MacroType type, string command)
    {
        Name = name;
        ImageUrl = imageUrl;
        Type = type;
        Command = command;
        UpdatedAt = DateTime.UtcNow;
    }

    // -1 = не назначен ни на один слот хотбара. R.1 — 0-29 = 3 страницы по 10 слотов (страница =
    // slot/10, позиция внутри страницы = slot%10) — единое число хранения проще, чем отдельное
    // поле страницы, и не потребовало миграции при добавлении многостраничности.
    public void SetHotbarSlot(int slot)
    {
        HotbarSlot = slot;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum MacroType { Chat, Script }
