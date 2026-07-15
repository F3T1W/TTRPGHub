using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class Pf2eLookupsTests
{
    [Theory]
    [InlineData("dragon,huge", "/img/tokens/dragon.svg")]
    [InlineData("undead, humanoid", "/img/tokens/undead.svg")]
    [InlineData(null, "/img/tokens/generic.svg")]
    [InlineData("", "/img/tokens/generic.svg")]
    [InlineData("swashbuckler", "/img/tokens/generic.svg")]
    public void MonsterPlaceholderIcon_ReturnsExpectedIcon(string? traits, string expected)
    {
        Assert.Equal(expected, Pf2eLookups.MonsterPlaceholderIcon(traits));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(6, 7)]
    [InlineData(8, 15)]
    public void MinLevelForRank_ReturnsExpectedThreshold(int rank, int expected)
    {
        Assert.Equal(expected, Pf2eLookups.MinLevelForRank(rank));
    }

    [Fact]
    public void Bonus_Untrained_IsAlwaysZero()
    {
        Assert.Equal(0, Pf2eLookups.Bonus(0, 20));
    }

    [Theory]
    [InlineData(2, 5, false, 7)]
    [InlineData(2, 5, true, 2)]
    [InlineData(6, 10, false, 16)]
    public void Bonus_TrainedOrHigher_AddsLevelUnlessProficiencyWithoutLevel(int rank, int level, bool withoutLevel, int expected)
    {
        Assert.Equal(expected, Pf2eLookups.Bonus(rank, level, withoutLevel));
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(1, false, -5)]
    [InlineData(2, false, -10)]
    [InlineData(1, true, -4)]
    [InlineData(2, true, -8)]
    public void MapPenalty_ReturnsExpectedPenalty(int strikeIndex, bool agile, int expected)
    {
        Assert.Equal(expected, Pf2eLookups.MapPenalty(strikeIndex, agile));
    }

    [Fact]
    public void SpellAttackBonus_CombinesProficiencyAndAbilityMod()
    {
        Assert.Equal(2 + 5 + 4, Pf2eLookups.SpellAttackBonus(2, 5, 4));
    }

    [Fact]
    public void SpellDc_AddsTenToAttackBonus()
    {
        var attack = Pf2eLookups.SpellAttackBonus(2, 5, 4);
        Assert.Equal(10 + attack, Pf2eLookups.SpellDc(2, 5, 4));
    }

    [Theory]
    [InlineData(Pf2eLookups.AbpPotency.Attack, 1, 0)]
    [InlineData(Pf2eLookups.AbpPotency.Attack, 2, 1)]
    [InlineData(Pf2eLookups.AbpPotency.Attack, 15, 2)]
    [InlineData(Pf2eLookups.AbpPotency.Attack, 16, 3)]
    [InlineData(Pf2eLookups.AbpPotency.Defense, 5, 1)]
    [InlineData(Pf2eLookups.AbpPotency.Save, 8, 1)]
    [InlineData(Pf2eLookups.AbpPotency.Perception, 7, 1)]
    public void AbpBonus_ReturnsHighestUnlockedTier(Pf2eLookups.AbpPotency potency, int level, int expected)
    {
        Assert.Equal(expected, Pf2eLookups.AbpBonus(potency, level));
    }

    [Fact]
    public void ComputeArmorClass_UnarmoredUsesFullDexMod()
    {
        var ac = Pf2eLookups.ComputeArmorClass(
            dexMod: 3, armorProficiencyRanks: new Dictionary<string, int> { ["unarmored"] = 2 },
            level: 5, proficiencyWithoutLevel: false, armor: null, automaticBonusProgression: false);

        Assert.Equal(10 + 3 + (2 + 5), ac);
    }

    [Fact]
    public void ComputeArmorClass_ArmorDexCapLimitsBonus()
    {
        var armor = new Pf2eLookups.EquippedItemContext(
            Slug: "chain-shirt", ItemKind: null, ArmorCategory: "light",
            Traits: [], IsRanged: false, DamageCategory: null, DexCap: 1, AcBonus: 4);
        var ac = Pf2eLookups.ComputeArmorClass(
            dexMod: 5, armorProficiencyRanks: new Dictionary<string, int> { ["light"] = 2 },
            level: 5, proficiencyWithoutLevel: false, armor: armor, automaticBonusProgression: false);

        Assert.Equal(10 + 1 + (2 + 5) + 4, ac);
    }

    [Fact]
    public void ComputeArmorClass_AutomaticBonusProgressionAddsDefensePotency()
    {
        var withoutAbp = Pf2eLookups.ComputeArmorClass(0, [], 5, false, null, automaticBonusProgression: false);
        var withAbp = Pf2eLookups.ComputeArmorClass(0, [], 5, false, null, automaticBonusProgression: true);

        Assert.Equal(withoutAbp + 1, withAbp);
    }

    [Fact]
    public void MonsterAbilityDc_UsesBestMentalModifier()
    {
        // Int 10 (+0), Wis 16 (+3), Cha 8 (-1) at level 4 -> 10 + 2 + 3
        Assert.Equal(15, Pf2eLookups.MonsterAbilityDc(4, 10, 16, 8));
    }

    [Fact]
    public void ParseMonsterAttacks_NullOrEmpty_ReturnsEmptyList()
    {
        Assert.Empty(Pf2eLookups.ParseMonsterAttacks(null));
        Assert.Empty(Pf2eLookups.ParseMonsterAttacks(""));
    }

    [Fact]
    public void ParseMonsterAttacks_MalformedJson_ReturnsEmptyList()
    {
        Assert.Empty(Pf2eLookups.ParseMonsterAttacks("not json"));
    }

    [Fact]
    public void ParseMonsterAttacks_ValidJson_ReturnsParsedAttacks()
    {
        var json = """[{"name":"Claw","bonus":8,"damageDice":"1d6","damageBonus":4,"damageType":"slashing"}]""";

        var attacks = Pf2eLookups.ParseMonsterAttacks(json);

        Assert.Single(attacks);
        Assert.Equal("Claw", attacks[0].Name);
        Assert.Equal(8, attacks[0].Bonus);
    }

    [Fact]
    public void ApplyDamageAdjustment_ResistanceReducesDamageNotBelowZero()
    {
        Assert.Equal(0, Pf2eLookups.ApplyDamageAdjustment(rawDamage: 5, resistance: 10, weakness: null));
        Assert.Equal(2, Pf2eLookups.ApplyDamageAdjustment(rawDamage: 5, resistance: 3, weakness: null));
    }

    [Fact]
    public void ApplyDamageAdjustment_WeaknessIncreasesDamage()
    {
        Assert.Equal(10, Pf2eLookups.ApplyDamageAdjustment(rawDamage: 5, resistance: null, weakness: 5));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("self", null)]
    [InlineData("unlimited", null)]
    [InlineData("touch", 5)]
    [InlineData("30 feet", 30)]
    [InlineData("1 mile", 5280)]
    [InlineData("0 feet", 5)]
    [InlineData("60 feet (see text)", 60)]
    public void ParseRangeFeet_ReturnsExpectedDistance(string? rangeText, int? expected)
    {
        Assert.Equal(expected, Pf2eLookups.ParseRangeFeet(rangeText));
    }

    [Theory]
    [InlineData(30, null, 30)]
    [InlineData(null, 10, 10)]
    [InlineData(null, null, 5)]
    public void AttackMaxRangeFeet_PrefersRangeOverReachOverDefault(int? range, int? reach, int expected)
    {
        Assert.Equal(expected, Pf2eLookups.AttackMaxRangeFeet(range, reach));
    }

    [Fact]
    public void ParseWeaponRangeFromTraits_ReachTraitSetsReach()
    {
        var (range, reach) = Pf2eLookups.ParseWeaponRangeFromTraits(["reach", "trip"]);

        Assert.Null(range);
        Assert.Equal(10, reach);
    }

    [Fact]
    public void ParseWeaponRangeFromTraits_ThrownTraitSetsRange()
    {
        var (range, reach) = Pf2eLookups.ParseWeaponRangeFromTraits(["thrown-20"]);

        Assert.Equal(20, range);
        Assert.Null(reach);
    }

    [Fact]
    public void ParseWeaponRangeFromTraitsString_ParsesCommaSeparatedTraits()
    {
        var (range, reach) = Pf2eLookups.ParseWeaponRangeFromTraitsString("thrown-10, agile");

        Assert.Equal(10, range);
        Assert.Null(reach);
    }

    [Fact]
    public void TokenDistanceFeet_AdjacentTokens_ReturnsFiveFeet()
    {
        var distance = Pf2eLookups.TokenDistanceFeet(0, 0, 1, 1, 1, 0, 1, 1);

        Assert.Equal(5, distance, precision: 5);
    }

    [Fact]
    public void ExpectedFreeArchetypeFeats_HalfOfLevelRoundedDown()
    {
        Assert.Equal(0, Pf2eLookups.ExpectedFreeArchetypeFeats(1));
        Assert.Equal(5, Pf2eLookups.ExpectedFreeArchetypeFeats(10));
        Assert.Equal(10, Pf2eLookups.ExpectedFreeArchetypeFeats(20));
    }

    [Fact]
    public void PendingGradualAbilityBoosts_ReturnsUnloggedLevelsUpToCurrent()
    {
        var pending = Pf2eLookups.PendingGradualAbilityBoosts(5, [1, 3]);

        Assert.Equal([2, 4, 5], pending);
    }

    [Theory]
    [InlineData(15, 17)]
    [InlineData(18, 19)]
    public void ApplyAbilityBoost_SmallerIncrementAtEighteenOrAbove(int score, int expected)
    {
        Assert.Equal(expected, Pf2eLookups.ApplyAbilityBoost(score));
    }

    [Fact]
    public void DueStandardAbilityBoostLevels_ReturnsUnloggedMilestonesUpToNewLevel()
    {
        var due = Pf2eLookups.DueStandardAbilityBoostLevels(12, [5]);

        Assert.Equal([10], due);
    }

    [Fact]
    public void NewFeatSlotsBetweenLevels_ReturnsSlotsInLevelOrder()
    {
        var slots = Pf2eLookups.NewFeatSlotsBetweenLevels(1, 2);

        Assert.Contains(slots, s => s.Level == 2 && s.Label == "Навыковый фит");
        Assert.Contains(slots, s => s.Level == 2 && s.Label == "Классовый фит");
        Assert.DoesNotContain(slots, s => s.Level == 1);
    }
}
