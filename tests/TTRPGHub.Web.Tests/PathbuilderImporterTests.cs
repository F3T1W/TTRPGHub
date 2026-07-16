using System.Text.Json;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class PathbuilderImporterTests
{
    [Fact]
    public void IsPathbuilderExport_HasBuildObject_ReturnsTrue()
    {
        using var doc = JsonDocument.Parse("""{"success":true,"build":{}}""");

        Assert.True(PathbuilderImporter.IsPathbuilderExport(doc.RootElement));
    }

    [Theory]
    [InlineData("""{"success":true}""")]
    [InlineData("""[]""")]
    [InlineData("""{"build":"not an object"}""")]
    public void IsPathbuilderExport_MissingOrWrongShapedBuild_ReturnsFalse(string json)
    {
        using var doc = JsonDocument.Parse(json);

        Assert.False(PathbuilderImporter.IsPathbuilderExport(doc.RootElement));
    }

    [Fact]
    public void Parse_MinimalBuild_FillsDefaults()
    {
        using var doc = JsonDocument.Parse("""{"build":{"name":"Grog","level":3}}""");

        var (request, stats) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal("Grog", request.Name);
        Assert.Equal(3, request.Level);
        Assert.Equal("—", request.Race);
        Assert.Equal("—", request.Class);
        Assert.Equal(10, request.Strength);
        Assert.Equal("str", stats.KeyAbility);
        Assert.Equal(0, stats.PerceptionRank);
    }

    [Fact]
    public void Parse_MissingName_FallsBackToPlaceholder()
    {
        using var doc = JsonDocument.Parse("""{"build":{}}""");

        var (request, _) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal("Без имени", request.Name);
    }

    [Fact]
    public void Parse_LevelOutOfRange_IsClampedToOneTwenty()
    {
        using var doc = JsonDocument.Parse("""{"build":{"level":99}}""");

        var (request, _) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal(20, request.Level);
    }

    [Fact]
    public void Parse_AbilitiesAndAttributes_ComputesHpAndSpeed()
    {
        var json = """
        {
          "build": {
            "name": "Grog", "level": 5,
            "abilities": {"str": 18, "dex": 14, "con": 16, "int": 10, "wis": 12, "cha": 8},
            "attributes": {"ancestryhp": 8, "classhp": 10, "bonushpPerLevel": 0, "bonushp": 2, "speed": 25, "speedBonus": 5}
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (request, _) = PathbuilderImporter.Parse(doc.RootElement);

        // 8 + 5 * (10 + 0 + conMod(3)) + 2 = 8 + 65 + 2 = 75
        Assert.Equal(75, request.MaxHitPoints);
        Assert.Equal(75, request.CurrentHitPoints);
        Assert.Equal(30, request.Speed);
        Assert.Equal(18, request.Strength);
        Assert.Equal(16, request.Constitution);
    }

    [Fact]
    public void Parse_Proficiencies_PopulatesRanksClampedToEight()
    {
        var json = """
        {
          "build": {
            "proficiencies": {"perception": 6, "classDC": 4, "fortitude": 99, "reflex": 2, "will": 0}
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal(6, stats.PerceptionRank);
        Assert.Equal(4, stats.ClassDcRank);
        Assert.Equal(8, stats.SaveRanks["fortitude"]);
        Assert.Equal(2, stats.SaveRanks["reflex"]);
        Assert.Equal(0, stats.SaveRanks["will"]);
    }

    [Fact]
    public void Parse_Feats_AddsNamedFeatsWithLevel()
    {
        var json = """{"build":{"feats":[["Power Attack","",null,3],["No Level Feat"]]}}""";
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal(2, stats.Feats.Count);
        Assert.Equal("Power Attack", stats.Feats[0].Name);
        Assert.Equal(3, stats.Feats[0].Level);
        Assert.Equal("No Level Feat", stats.Feats[1].Name);
        Assert.Equal(1, stats.Feats[1].Level);
    }

    [Fact]
    public void Parse_Equipment_AddsToInventoryAndEquipmentText()
    {
        var json = """{"build":{"equipment":[["Rope",1],["Torch",5]]}}""";
        using var doc = JsonDocument.Parse(json);

        var (request, stats) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal(2, stats.Inventory.Count);
        Assert.Contains("Rope", request.Equipment);
        Assert.Contains("Torch ×5", request.Equipment);
    }

    [Fact]
    public void Parse_Weapon_PicksDexWhenAttackBonusMatchesDexMod()
    {
        var json = """
        {
          "build": {
            "level": 5,
            "abilities": {"str": 10, "dex": 18},
            "proficiencies": {"martial": 4},
            "weapons": [{"name":"Rapier","display":"Rapier +1","prof":"martial","die":"d6","str":"","pot":0,"attack":13,"damageBonus":4,"damageType":"P"}]
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = PathbuilderImporter.Parse(doc.RootElement);

        var attack = Assert.Single(stats.Attacks);
        Assert.Equal("Rapier", attack.Name);
        Assert.Equal("dex", attack.AbilityKey);
        Assert.Equal("piercing", attack.DamageType);
    }

    [Fact]
    public void Parse_Money_JoinsNonZeroDenominationsIntoEquipmentText()
    {
        var json = """{"build":{"money":{"pp":0,"gp":15,"sp":3,"cp":0}}}""";
        using var doc = JsonDocument.Parse(json);

        var (request, _) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal("15 зм, 3 см", request.Equipment);
    }

    [Fact]
    public void Parse_SpellCasters_PopulatesTraditionRankAndKnownSpells()
    {
        var json = """
        {
          "build": {
            "spellCasters": [
              {
                "magicTradition": "arcane", "proficiency": 4,
                "perDay": [3, 2, 0],
                "spells": [{"spellLevel": 0, "list": ["Prestidigitation"]}, {"spellLevel": 1, "list": ["Magic Missile", "Shield"]}]
              }
            ]
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var (_, stats) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal("arcane", stats.SpellcastingTradition);
        Assert.Equal(4, stats.SpellcastingRank);
        Assert.Equal(3, stats.SpellSlots[0].Max);
        Assert.Equal(2, stats.SpellSlots[1].Max);
        Assert.Equal(3, stats.KnownSpells.Count);
        Assert.Contains(stats.KnownSpells, s => s.Name == "Magic Missile" && s.Level == 1);
    }

    [Fact]
    public void Parse_FocusPoints_AddsResourceWhenPositive()
    {
        using var doc = JsonDocument.Parse("""{"build":{"focusPoints":2}}""");

        var (_, stats) = PathbuilderImporter.Parse(doc.RootElement);

        var resource = Assert.Single(stats.Resources);
        Assert.Equal("Focus Points", resource.Name);
        Assert.Equal(2, resource.Max);
    }

    [Fact]
    public void Parse_HeritageAndSpecials_BuildFeaturesText()
    {
        var json = """{"build":{"heritage":"Rock Dwarf","specials":["Darkvision","Stonecunning"]}}""";
        using var doc = JsonDocument.Parse(json);

        var (request, _) = PathbuilderImporter.Parse(doc.RootElement);

        Assert.Equal("Rock Dwarf\nDarkvision\nStonecunning", request.FeaturesAndTraits);
    }
}
