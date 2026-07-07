using System.Text.Json;
using TTRPGHub.Services;

var json = await File.ReadAllTextAsync(args.Length > 0
    ? args[0]
    // AppContext.BaseDirectory = scripts/FoundryImportSmoke/bin/Debug/net10.0/ — 5 уровней
    // вверх до корня репозитория (было 4 — путь резолвился в несуществующий scripts/tests/...).
    : Path.Combine(AppContext.BaseDirectory, "../../../../../tests/fixtures/foundry-champion-l5.json"));

using var doc = JsonDocument.Parse(json);
var root = doc.RootElement;

if (!FoundryActorImporter.IsFoundryActorExport(root))
{
    Console.Error.WriteLine("FAIL: not recognized as Foundry export");
    return 1;
}

var (request, stats) = FoundryActorImporter.Parse(root);

var checks = new (string Name, bool Ok)[]
{
    ("name", request.Name == "E2E Foundry Champion"),
    ("race", request.Race == "Human"),
    ("class", request.Class == "Champion"),
    ("level", request.Level == 5),
    ("hp", request.MaxHitPoints == 68 && request.CurrentHitPoints == 68),
    ("ac", request.ArmorClass == 23),
    ("speed", request.Speed == 20),
    ("str", request.Strength == 18),
    ("feats", stats.Feats.Count == 1 && stats.Feats[0].Slug == "power-attack"),
    ("attacks", stats.Attacks.Count == 1 && stats.Attacks[0].DamageDice == "2d8"),
    ("inventory", stats.Inventory.Count >= 2),
    ("spells", stats.KnownSpells.Any(s => s.Name == "Lay on Hands")),
    ("tradition", stats.SpellcastingTradition == "divine"),
};

var failed = checks.Where(c => !c.Ok).ToList();
foreach (var c in checks)
    Console.WriteLine(c.Ok ? $"OK  {c.Name}" : $"FAIL {c.Name}");

if (failed.Count > 0) return 1;
Console.WriteLine("All parser checks passed.");
if (args.Contains("--print-stats"))
    Console.WriteLine(stats.ToJson());
return 0;
