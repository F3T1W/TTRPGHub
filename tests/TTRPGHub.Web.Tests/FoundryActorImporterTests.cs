using System.Text.Json;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class FoundryActorImporterTests
{
    [Fact]
    public void IsFoundryActorExport_CharacterWithDetails_ReturnsTrue()
    {
        using var doc = JsonDocument.Parse("""{"type":"character","system":{"details":{}}}""");

        Assert.True(FoundryActorImporter.IsFoundryActorExport(doc.RootElement));
    }

    [Theory]
    [InlineData("""{"type":"npc","system":{"details":{}}}""")]
    [InlineData("""{"type":"character"}""")]
    [InlineData("""{"type":"character","system":{}}""")]
    [InlineData("""[]""")]
    public void IsFoundryActorExport_WrongTypeOrMissingSystemDetails_ReturnsFalse(string json)
    {
        using var doc = JsonDocument.Parse(json);

        Assert.False(FoundryActorImporter.IsFoundryActorExport(doc.RootElement));
    }

    [Fact]
    public void Parse_MinimalCharacter_UsesDefaults()
    {
        using var doc = JsonDocument.Parse("""{"name":"Grog","type":"character","system":{"details":{"level":{"value":3}}}}""");

        var (request, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal("Grog", request.Name);
        Assert.Equal(3, request.Level);
        Assert.Equal("—", request.Race);
        Assert.Equal("—", request.Class);
        Assert.Equal(10, request.Strength);
        Assert.Equal(1, request.MaxHitPoints);
        Assert.Equal("str", stats.KeyAbility);
    }

    [Fact]
    public void Parse_MissingName_FallsBackToPlaceholder()
    {
        using var doc = JsonDocument.Parse("""{"type":"character","system":{"details":{}}}""");

        var (request, _) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal("Без имени", request.Name);
    }

    [Fact]
    public void Parse_LevelOutOfRange_IsClamped()
    {
        using var doc = JsonDocument.Parse("""{"type":"character","system":{"details":{"level":{"value":50}}}}""");

        var (request, _) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal(20, request.Level);
    }

    [Fact]
    public void Parse_AbilityScores_PrefersValueThenBaseThenMod()
    {
        var json = """
        {
          "type": "character",
          "system": {
            "details": {},
            "abilities": {
              "str": {"value": 18},
              "dex": {"base": 14},
              "con": {"mod": 3},
              "int": {}
            }
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (request, _) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal(18, request.Strength);
        Assert.Equal(14, request.Dexterity);
        Assert.Equal(16, request.Constitution); // mod 3 -> 3*2+10
        Assert.Equal(10, request.Intelligence);
    }

    [Fact]
    public void Parse_HpAcSpeed_ReadFromAttributes()
    {
        var json = """
        {
          "type": "character",
          "system": {
            "details": {},
            "attributes": {"hp": {"max": 42, "value": 30}, "ac": {"value": 18}, "speed": {"value": 30}}
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (request, _) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal(42, request.MaxHitPoints);
        Assert.Equal(30, request.CurrentHitPoints);
        Assert.Equal(18, request.ArmorClass);
        Assert.Equal(30, request.Speed);
    }

    [Fact]
    public void Parse_CurrentHpAboveMax_IsClampedToMax()
    {
        var json = """{"type":"character","system":{"details":{},"attributes":{"hp":{"max":10,"value":999}}}}""";
        using var doc = JsonDocument.Parse(json);

        var (request, _) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal(10, request.CurrentHitPoints);
    }

    [Fact]
    public void Parse_MovementLandSpeed_TakesPriorityOverAttributesSpeed()
    {
        var json = """
        {
          "type": "character",
          "system": {
            "details": {},
            "movement": {"speeds": {"land": {"value": 35}}},
            "attributes": {"speed": {"value": 25}}
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (request, _) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal(35, request.Speed);
    }

    [Fact]
    public void Parse_AncestryHeritageClassBackgroundItems_PopulateRequestFields()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [
            {"type": "ancestry", "name": "Dwarf"},
            {"type": "heritage", "name": "Rock Dwarf"},
            {"type": "class", "name": "Fighter", "system": {"hp": 10}},
            {"type": "background", "name": "Blacksmith"}
          ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (request, _) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal("Dwarf", request.Race);
        Assert.Equal("Fighter", request.Class);
        Assert.Equal("Blacksmith", request.Background);
        Assert.Equal("Rock Dwarf", request.FeaturesAndTraits);
        Assert.Equal($"{1}d10", request.HitDice);
    }

    [Fact]
    public void Parse_FeatItem_AddsFeatWithLevelAndSlug()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [{"type": "feat", "name": "Power Attack", "system": {"level": {"value": 1}}}]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        var feat = Assert.Single(stats.Feats);
        Assert.Equal("Power Attack", feat.Name);
        Assert.Equal(1, feat.Level);
        Assert.NotNull(feat.Slug);
    }

    [Fact]
    public void Parse_UnequippedWeapon_IsSkippedEntirely()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [{"type": "weapon", "name": "Spare Sword", "system": {"equipped": {"carryType": "stowed"}}}]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Empty(stats.Attacks);
        Assert.Empty(stats.Inventory);
    }

    [Fact]
    public void Parse_EquippedWeapon_AddsInventoryAndAttack()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [{
            "type": "weapon", "name": "Longsword",
            "system": {
              "equipped": {"carryType": "held"},
              "category": "martial",
              "damage": {"die": "d8", "dice": 1, "modifier": 3, "damageType": "slashing"},
              "attribute": "str"
            }
          }]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Single(stats.Inventory);
        var attack = Assert.Single(stats.Attacks);
        Assert.Equal("Longsword", attack.Name);
        Assert.Equal("1d8", attack.DamageDice);
        Assert.Equal(3, attack.DamageBonus);
        Assert.Equal("slashing", attack.DamageType);
        Assert.Equal("str", attack.AbilityKey);
        // "martial" with no explicit proficiency data defaults to rank 2 (trained).
        Assert.Equal(2, attack.Rank);
    }

    [Fact]
    public void Parse_ArmorItem_TracksEquippedState()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [
            {"type": "armor", "name": "Worn Chain Shirt", "system": {"equipped": {"carryType": "worn"}}},
            {"type": "shield", "name": "Stowed Shield", "system": {"equipped": {"carryType": "stowed"}}}
          ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal(2, stats.Inventory.Count);
        Assert.True(stats.Inventory.Single(i => i.Name == "Worn Chain Shirt").Equipped);
        Assert.False(stats.Inventory.Single(i => i.Name == "Stowed Shield").Equipped);
    }

    [Fact]
    public void Parse_EquipmentItem_UsesQuantityFromSystem()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [{"type": "consumable", "name": "Potion of Healing", "system": {"quantity": 3}}]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        var item = Assert.Single(stats.Inventory);
        Assert.Equal("Potion of Healing", item.Name);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public void Parse_SpellItem_AddsKnownSpellAtCorrectLevel()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [{"type": "spell", "name": "Fireball", "system": {"level": {"value": 3}}}]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        var spell = Assert.Single(stats.KnownSpells);
        Assert.Equal("Fireball", spell.Name);
        Assert.Equal(3, spell.Level);
        Assert.False(spell.Prepared);
    }

    [Fact]
    public void Parse_SpellcastingEntry_PopulatesTraditionRankAndSlots()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [{
            "type": "spellcastingEntry", "name": "Arcane Spellcasting",
            "system": {
              "tradition": {"value": "arcane"},
              "proficiency": {"value": 2},
              "slots": {"slot1": {"max": 3}, "slot0": {"max": 5}, "notASlot": {"max": 99}}
            }
          }]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal("arcane", stats.SpellcastingTradition);
        Assert.Equal(4, stats.SpellcastingRank); // MapRank(2) = 2*2 = 4
        Assert.Equal(3, stats.SpellSlots[1].Max);
        Assert.Equal(5, stats.SpellSlots[0].Max);
    }

    [Fact]
    public void Parse_SecondSpellcastingEntry_DoesNotOverwriteFirst()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": [
            {"type": "spellcastingEntry", "name": "First", "system": {"tradition": {"value": "arcane"}}},
            {"type": "spellcastingEntry", "name": "Second", "system": {"tradition": {"value": "divine"}}}
          ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal("arcane", stats.SpellcastingTradition);
    }

    [Fact]
    public void Parse_FocusResource_AddedWhenMaxPositive()
    {
        var json = """{"type":"character","system":{"details":{},"resources":{"focus":{"value":1,"max":3}}}}""";
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        var resource = Assert.Single(stats.Resources);
        Assert.Equal("Focus Points", resource.Name);
        Assert.Equal(1, resource.Current);
        Assert.Equal(3, resource.Max);
    }

    [Fact]
    public void Parse_ItemsAsObjectMap_AreEnumeratedLikeAnArray()
    {
        var json = """
        {
          "type": "character",
          "system": {"details": {}},
          "items": {"abc123": {"type": "feat", "name": "Toughness", "system": {"level": {"value": 1}}}}
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Single(stats.Feats);
        Assert.Equal("Toughness", stats.Feats[0].Name);
    }

    [Fact]
    public void Parse_ClassDcRank_PrefersPrimaryFlaggedEntry()
    {
        var json = """
        {
          "type": "character",
          "system": {
            "details": {},
            "proficiencies": {
              "classDCs": {
                "fighter": {"rank": 1, "primary": false},
                "archer": {"rank": 3, "primary": true}
              }
            }
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = FoundryActorImporter.Parse(doc.RootElement);

        Assert.Equal(6, stats.ClassDcRank); // MapRank(3) = 3*2 = 6
    }
}
