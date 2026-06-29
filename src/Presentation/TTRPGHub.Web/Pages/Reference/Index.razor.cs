using Microsoft.AspNetCore.Authorization;

namespace TTRPGHub.Pages.Reference;

public partial class Index
{
    private readonly List<GameSystemCard> _systems =
    [
        new("D&D 5e", "🐉", "/reference/dnd5e/spells",
            "Dungeons & Dragons 5-е издание — самая популярная НРИ в мире. Полный SRD: заклинания, монстры, снаряжение.",
            ["Заклинания", "Бестиарий"], Available: true),

        new("Pathfinder 2e", "⚔️", "/reference/pf2e",
            "Pathfinder 2nd Edition — глубокая система с богатой тактикой и огромным выбором вариантов персонажа.",
            ["Заклинания", "Бестиарий", "Черты"], Available: false),

        new("Cyberpunk RED", "🤖", "/reference/cpred",
            "Тёмное киберпанк-будущее. Хакеры, наёмники и корпоративные войны в системе Cyberpunk RED.",
            ["Снаряжение", "Нетраннинг", "Оружие"], Available: false),

        new("Call of Cthulhu", "🐙", "/reference/coc",
            "Хоррор-расследования в мире Лавкрафта. Система BRP: навыки, безумие, космический ужас.",
            ["Навыки", "Заклинания", "Бестиарий"], Available: false),

        new("Shadowrun", "🌆", "/reference/shadowrun",
            "Магия встречает технологии. Эльфы-хакеры, тролли-самураи и мегакорпорации в 2080-х.",
            ["Снаряжение", "Магия", "Матрица"], Available: false),

        new("GURPS", "📚", "/reference/gurps",
            "Generic Universal RolePlaying System — универсальная система для любого жанра и сеттинга.",
            ["Навыки", "Преимущества", "Снаряжение"], Available: false),
    ];

    private sealed record GameSystemCard(
        string Name, string Icon, string Url,
        string Description, List<string> Tags, bool Available);
}
