using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Seeding;

public static class ForumSeeder
{
    private static readonly (string Name, string Description, string Slug, int Order)[] DefaultCategories =
    [
        ("Общее", "Всё что не вписывается в другие категории", "general", 0),
        ("D&D 5e", "Обсуждение Dungeons & Dragons 5-й редакции", "dnd5e", 1),
        ("Pathfinder", "Pathfinder 1e и 2e", "pathfinder", 2),
        ("Другие системы", "GURPS, CoC, Fate, Savage Worlds и другие", "other-systems", 3),
        ("Поиск группы", "Ищу игроков / ищу мастера", "lfg", 4),
        ("Homebrew", "Самодельные материалы и правила", "homebrew", 5),
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.ForumCategories.AnyAsync(ct))
            return;

        foreach (var (name, desc, slug, order) in DefaultCategories)
            db.ForumCategories.Add(ForumCategory.Create(name, desc, slug, order));

        await db.SaveChangesAsync(ct);
    }
}
