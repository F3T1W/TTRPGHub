using System.Text.Json;

namespace TTRPGHub.Services;

// J.8 — импорт листа персонажа из Pathbuilder 2e ("Export to JSON" в приложении/на сайте).
// Формат экспорта: { "success": true, "build": { ... } }. Разбор целиком на клиенте: сервер
// переиспользует существующие эндпоинты /api/characters/import + /pf2e-stats, отдельного
// серверного парсера не нужно. Ранги владения Pathbuilder (0/2/4/6/8) совпадают с нашими
// (Pf2eLookups.Ranks) — копируются без преобразования.
public static class PathbuilderImporter
{
    public static bool IsPathbuilderExport(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty("build", out var b) && b.ValueKind == JsonValueKind.Object;

    public static (ImportCharacterRequest Request, Pf2eLookups.Pf2eStatsModel Stats) Parse(JsonElement root)
    {
        var build = root.GetProperty("build");

        var name = GetString(build, "name") ?? "Без имени";
        var level = Math.Clamp(GetInt(build, "level", 1), 1, 20);
        var abilities = build.TryGetProperty("abilities", out var ab) ? ab : default;
        int Score(string key) => ab.ValueKind == JsonValueKind.Object ? GetInt(abilities, key, 10) : 10;

        var str = Score("str");
        var dex = Score("dex");
        var con = Score("con");
        var conMod = (con - 10) / 2;

        // HP по правилам PF2e: анцестри + (класс + бонус за уровень + мод Телосложения) × уровень.
        var attrs = build.TryGetProperty("attributes", out var at) ? at : default;
        var maxHp = attrs.ValueKind == JsonValueKind.Object
            ? GetInt(attrs, "ancestryhp", 8)
              + level * (GetInt(attrs, "classhp", 8) + GetInt(attrs, "bonushpPerLevel", 0) + conMod)
              + GetInt(attrs, "bonushp", 0)
            : 8 + level * (8 + conMod);
        var speed = attrs.ValueKind == JsonValueKind.Object
            ? GetInt(attrs, "speed", 25) + GetInt(attrs, "speedBonus", 0)
            : 25;

        var ac = build.TryGetProperty("acTotal", out var acEl) && acEl.ValueKind == JsonValueKind.Object
            ? GetInt(acEl, "acTotal", 10)
            : 10;

        var prof = build.TryGetProperty("proficiencies", out var pr) && pr.ValueKind == JsonValueKind.Object ? pr : default;
        int Rank(string key) => prof.ValueKind == JsonValueKind.Object ? Math.Clamp(GetInt(prof, key, 0), 0, 8) : 0;

        var stats = new Pf2eLookups.Pf2eStatsModel
        {
            KeyAbility = GetString(build, "keyability") ?? "str",
            PerceptionRank = Rank("perception"),
            ClassDcRank = Rank("classDC"),
            SaveRanks = new Dictionary<string, int>
            {
                ["fortitude"] = Rank("fortitude"),
                ["reflex"] = Rank("reflex"),
                ["will"] = Rank("will"),
            },
        };

        foreach (var (key, _) in Pf2eLookups.Skills)
        {
            var rank = Rank(key);
            if (rank > 0) stats.SkillRanks[key] = rank;
        }

        // Отдельных Lore-навыков (Warfare Lore и т.д.) у нас нет — один общий ключ "lore",
        // берём лучший из имеющихся, названия конкретных знаний уходят в FeaturesAndTraits ниже.
        var loreNames = new List<string>();
        if (build.TryGetProperty("lores", out var lores) && lores.ValueKind == JsonValueKind.Array)
        {
            foreach (var lore in lores.EnumerateArray())
            {
                if (lore.ValueKind != JsonValueKind.Array || lore.GetArrayLength() < 2) continue;
                loreNames.Add($"{lore[0].GetString()} Lore");
                var rank = Math.Clamp(lore[1].GetInt32(), 0, 8);
                if (rank > stats.SkillRanks.GetValueOrDefault("lore")) stats.SkillRanks["lore"] = rank;
            }
        }

        if (build.TryGetProperty("feats", out var feats) && feats.ValueKind == JsonValueKind.Array)
        {
            foreach (var feat in feats.EnumerateArray())
            {
                if (feat.ValueKind != JsonValueKind.Array || feat.GetArrayLength() < 1) continue;
                var featName = feat[0].GetString();
                if (string.IsNullOrWhiteSpace(featName)) continue;
                var featLevel = feat.GetArrayLength() >= 4 && feat[3].ValueKind == JsonValueKind.Number ? feat[3].GetInt32() : 1;
                stats.Feats.Add(new Pf2eLookups.Pf2eFeat(featName, featLevel));
            }
        }

        var equipmentNames = new List<string>();
        if (build.TryGetProperty("equipment", out var eq) && eq.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in eq.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 1) continue;
                var itemName = item[0].GetString();
                if (string.IsNullOrWhiteSpace(itemName)) continue;
                var qty = item.GetArrayLength() >= 2 && item[1].ValueKind == JsonValueKind.Number ? item[1].GetInt32() : 1;
                stats.Inventory.Add(new Pf2eLookups.Pf2eInventoryItem(itemName, qty, 0, false));
                equipmentNames.Add(qty > 1 ? $"{itemName} ×{qty}" : itemName);
            }
        }

        if (build.TryGetProperty("armor", out var armors) && armors.ValueKind == JsonValueKind.Array)
        {
            foreach (var armor in armors.EnumerateArray())
            {
                var display = GetString(armor, "display") ?? GetString(armor, "name");
                if (string.IsNullOrWhiteSpace(display)) continue;
                stats.Inventory.Add(new Pf2eLookups.Pf2eInventoryItem(
                    display, 1, 0, Equipped: armor.TryGetProperty("worn", out var w) && w.ValueKind == JsonValueKind.True));
                equipmentNames.Add(display);
            }
        }

        if (build.TryGetProperty("weapons", out var weapons) && weapons.ValueKind == JsonValueKind.Array)
        {
            foreach (var weapon in weapons.EnumerateArray())
            {
                var weaponName = GetString(weapon, "display") ?? GetString(weapon, "name");
                if (string.IsNullOrWhiteSpace(weaponName)) continue;

                stats.Inventory.Add(new Pf2eLookups.Pf2eInventoryItem(weaponName, 1, 0, true));
                equipmentNames.Add(weaponName);

                var weaponRank = Rank(GetString(weapon, "prof") ?? "simple");
                var die = GetString(weapon, "die") ?? "d6";
                var diceCount = (GetString(weapon, "str") ?? "") switch
                {
                    "striking" => 2, "greaterStriking" => 3, "majorStriking" => 4, _ => 1,
                };

                // Pathbuilder отдаёт готовый итоговый бонус атаки, а наша модель атаки считает его
                // из ранга+уровня+характеристики. Подбираем ту характеристику (str/dex), с которой
                // пересчёт совпадает с числом Pathbuilder за вычетом потенси-руны; не совпало
                // (нестандартные бонусы) — берём str по умолчанию.
                var pot = GetInt(weapon, "pot", 0);
                var targetBonus = GetInt(weapon, "attack", 0) - pot;
                var baseBonus = Pf2eLookups.Bonus(weaponRank, level);
                var abilityKey = targetBonus - baseBonus == (dex - 10) / 2 && dex != str ? "dex" : "str";

                stats.Attacks.Add(new Pf2eLookups.Pf2eAttack(
                    GetString(weapon, "name") ?? weaponName, weaponRank, abilityKey,
                    $"{diceCount}{die}", GetInt(weapon, "damageBonus", 0),
                    GetString(weapon, "damageType") switch
                    {
                        "B" => "bludgeoning", "P" => "piercing", "S" => "slashing",
                        var other => other?.ToLowerInvariant(),
                    }));
            }
        }

        if (build.TryGetProperty("money", out var money) && money.ValueKind == JsonValueKind.Object)
        {
            var coins = new[] { ("pp", "пм"), ("gp", "зм"), ("sp", "см"), ("cp", "мм") }
                .Select(c => (Amount: GetInt(money, c.Item1, 0), Label: c.Item2))
                .Where(c => c.Amount > 0)
                .Select(c => $"{c.Amount} {c.Label}")
                .ToList();
            if (coins.Count > 0) equipmentNames.Add(string.Join(", ", coins));
        }

        // Спеллкастинг: берём первый (основной) кастер-блок; perDay[i] — слоты уровня i (0 = заговоры).
        if (build.TryGetProperty("spellCasters", out var casters) && casters.ValueKind == JsonValueKind.Array)
        {
            foreach (var caster in casters.EnumerateArray())
            {
                if (stats.SpellcastingTradition is null)
                {
                    stats.SpellcastingTradition = GetString(caster, "magicTradition");
                    stats.SpellcastingRank = Math.Clamp(GetInt(caster, "proficiency", 0), 0, 8);

                    if (caster.TryGetProperty("perDay", out var perDay) && perDay.ValueKind == JsonValueKind.Array)
                    {
                        var slotLevel = 0;
                        foreach (var slots in perDay.EnumerateArray())
                        {
                            if (slots.ValueKind == JsonValueKind.Number && slots.GetInt32() > 0)
                                stats.SpellSlots[slotLevel] = new Pf2eLookups.Pf2eSpellSlotLevel(slots.GetInt32(), 0);
                            slotLevel++;
                        }
                    }
                }

                if (!caster.TryGetProperty("spells", out var spellGroups) || spellGroups.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var group in spellGroups.EnumerateArray())
                {
                    var spellLevel = GetInt(group, "spellLevel", 0);
                    if (!group.TryGetProperty("list", out var list) || list.ValueKind != JsonValueKind.Array) continue;
                    foreach (var spell in list.EnumerateArray())
                    {
                        var spellName = spell.GetString();
                        if (!string.IsNullOrWhiteSpace(spellName))
                            stats.KnownSpells.Add(new Pf2eLookups.Pf2eKnownSpell(spellName, spellLevel, false));
                    }
                }
            }
        }

        var focusPoints = GetInt(build, "focusPoints", 0);
        if (focusPoints > 0)
            stats.Resources.Add(new Pf2eLookups.Pf2eResource("Focus Points", focusPoints, focusPoints));

        var featureLines = new List<string>();
        if (build.TryGetProperty("specials", out var specials) && specials.ValueKind == JsonValueKind.Array)
            featureLines.AddRange(specials.EnumerateArray()
                .Select(s => s.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))!);
        featureLines.AddRange(loreNames);
        var heritage = GetString(build, "heritage");
        if (!string.IsNullOrWhiteSpace(heritage)) featureLines.Insert(0, heritage);

        var request = new ImportCharacterRequest(
            Name: name,
            Race: GetString(build, "ancestry") ?? "—",
            Class: GetString(build, "class") ?? "—",
            Level: level,
            Background: GetString(build, "background"),
            Alignment: GetString(build, "alignment"),
            Strength: str, Dexterity: dex, Constitution: con,
            Intelligence: Score("int"), Wisdom: Score("wis"), Charisma: Score("cha"),
            MaxHitPoints: Math.Max(1, maxHp), CurrentHitPoints: Math.Max(1, maxHp),
            ArmorClass: ac, Speed: speed,
            HitDice: attrs.ValueKind == JsonValueKind.Object ? $"{level}d{GetInt(attrs, "classhp", 8)}" : "1d8",
            SkillProficiencies: Pf2eLookups.Skills
                .Where(s => stats.SkillRanks.GetValueOrDefault(s.Key) > 0)
                .Select(s => s.Label).ToList(),
            SavingThrowProficiencies: [],
            FeaturesAndTraits: featureLines.Count > 0 ? string.Join("\n", featureLines) : null,
            Equipment: equipmentNames.Count > 0 ? string.Join("\n", equipmentNames) : null);

        return (request, stats);
    }

    private static string? GetString(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static int GetInt(JsonElement el, string prop, int fallback) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : fallback;
}
