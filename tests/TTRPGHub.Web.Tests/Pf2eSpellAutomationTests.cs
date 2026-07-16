using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class Pf2eSpellAutomationTests
{
    [Fact]
    public void ParseDamage_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(Pf2eSpellAutomation.ParseDamage(null));
        Assert.Null(Pf2eSpellAutomation.ParseDamage(""));
    }

    [Fact]
    public void ParseDamage_MalformedJson_ReturnsNull()
    {
        Assert.Null(Pf2eSpellAutomation.ParseDamage("not json"));
    }

    [Fact]
    public void ParseDamage_NoInstancesArray_ReturnsNull()
    {
        Assert.Null(Pf2eSpellAutomation.ParseDamage("""{"foo":"bar"}"""));
    }

    [Fact]
    public void ParseDamage_ValidInstances_ParsesFields()
    {
        var json = """{"instances":[{"formula":"2d6","type":"fire","kinds":["damage"],"applyMod":true}]}""";

        var damage = Pf2eSpellAutomation.ParseDamage(json);

        Assert.NotNull(damage);
        var instance = Assert.Single(damage.Instances);
        Assert.Equal("2d6", instance.Formula);
        Assert.Equal("fire", instance.Type);
        Assert.Equal(["damage"], instance.Kinds);
        Assert.True(instance.ApplyMod);
    }

    [Fact]
    public void ParseDamage_InstanceWithoutFormula_IsSkipped()
    {
        var json = """{"instances":[{"type":"fire"},{"formula":"1d4","type":"acid"}]}""";

        var damage = Pf2eSpellAutomation.ParseDamage(json);

        var instance = Assert.Single(damage!.Instances);
        Assert.Equal("1d4", instance.Formula);
    }

    [Fact]
    public void ParseHeightening_WrongType_ReturnsNull()
    {
        Assert.Null(Pf2eSpellAutomation.ParseHeightening("""{"type":"fixed"}"""));
    }

    [Fact]
    public void ParseHeightening_ValidInterval_ParsesIncrements()
    {
        var json = """{"type":"interval","interval":2,"damage":["1d6","1d4"]}""";

        var heightening = Pf2eSpellAutomation.ParseHeightening(json);

        Assert.NotNull(heightening);
        Assert.Equal(2, heightening.Interval);
        Assert.Equal(["1d6", "1d4"], heightening.DamageIncrements);
    }

    [Fact]
    public void ParseHeightening_NonPositiveInterval_DefaultsToOne()
    {
        var json = """{"type":"interval","interval":0,"damage":["1d6"]}""";

        var heightening = Pf2eSpellAutomation.ParseHeightening(json);

        Assert.Equal(1, heightening!.Interval);
    }

    [Fact]
    public void ParseDefense_MissingSave_ReturnsNull()
    {
        Assert.Null(Pf2eSpellAutomation.ParseDefense("""{"basic":true}"""));
    }

    [Fact]
    public void ParseDefense_ValidSave_ParsesBasicFlag()
    {
        var defense = Pf2eSpellAutomation.ParseDefense("""{"save":"reflex","basic":true}""");

        Assert.Equal("reflex", defense!.Save);
        Assert.True(defense.Basic);
    }

    [Fact]
    public void HeightenSteps_NoHeightening_ReturnsZero()
    {
        Assert.Equal(0, Pf2eSpellAutomation.HeightenSteps(3, 7, null));
    }

    [Fact]
    public void HeightenSteps_CastAtOrBelowBase_ReturnsZero()
    {
        var heightening = new Pf2eSpellAutomation.SpellHeightening(2, ["1d6"]);

        Assert.Equal(0, Pf2eSpellAutomation.HeightenSteps(5, 5, heightening));
        Assert.Equal(0, Pf2eSpellAutomation.HeightenSteps(5, 3, heightening));
    }

    [Fact]
    public void HeightenSteps_CastAboveBase_DividesByInterval()
    {
        var heightening = new Pf2eSpellAutomation.SpellHeightening(2, ["1d6"]);

        Assert.Equal(2, Pf2eSpellAutomation.HeightenSteps(3, 7, heightening));
    }

    [Fact]
    public void ResolveDamage_NoHeightening_ReturnsFormulaUnchanged()
    {
        var damage = new Pf2eSpellAutomation.SpellDamage([
            new Pf2eSpellAutomation.DamageInstance("2d6", "fire", ["damage"], ApplyMod: false)
        ]);

        var resolved = Pf2eSpellAutomation.ResolveDamage(damage, null, baseLevel: 1, castLevel: 1, abilityMod: 0);

        var instance = Assert.Single(resolved);
        Assert.Equal("2d6", instance.Expression);
        Assert.True(instance.IsDamage);
        Assert.False(instance.IsHealing);
        Assert.Equal("fire", instance.DamageType);
    }

    [Fact]
    public void ResolveDamage_HeightenedSpell_CombinesDiceOfSameFaces()
    {
        var damage = new Pf2eSpellAutomation.SpellDamage([
            new Pf2eSpellAutomation.DamageInstance("2d6", "fire", ["damage"], ApplyMod: false)
        ]);
        var heightening = new Pf2eSpellAutomation.SpellHeightening(1, ["1d6"]);

        // Base level 1, cast at level 3 -> 2 heighten steps, each adding 1d6.
        var resolved = Pf2eSpellAutomation.ResolveDamage(damage, heightening, baseLevel: 1, castLevel: 3, abilityMod: 0);

        Assert.Equal("4d6", resolved[0].Expression);
    }

    [Fact]
    public void ResolveDamage_ApplyModAddsAbilityModifierAsFlatBonus()
    {
        var damage = new Pf2eSpellAutomation.SpellDamage([
            new Pf2eSpellAutomation.DamageInstance("1d8", "cold", ["damage"], ApplyMod: true)
        ]);

        var resolved = Pf2eSpellAutomation.ResolveDamage(damage, null, baseLevel: 1, castLevel: 1, abilityMod: 4);

        Assert.Equal("1d8+4", resolved[0].Expression);
    }

    [Fact]
    public void ResolveDamage_NegativeAbilityModifier_SubtractsFromFormula()
    {
        var damage = new Pf2eSpellAutomation.SpellDamage([
            new Pf2eSpellAutomation.DamageInstance("1d8", "cold", ["damage"], ApplyMod: true)
        ]);

        var resolved = Pf2eSpellAutomation.ResolveDamage(damage, null, baseLevel: 1, castLevel: 1, abilityMod: -2);

        Assert.Equal("1d8-2", resolved[0].Expression);
    }

    [Fact]
    public void ResolveDamage_HealingKind_SetsIsHealingNotDamage()
    {
        var damage = new Pf2eSpellAutomation.SpellDamage([
            new Pf2eSpellAutomation.DamageInstance("2d8", null, ["healing"], ApplyMod: false)
        ]);

        var resolved = Pf2eSpellAutomation.ResolveDamage(damage, null, baseLevel: 1, castLevel: 1, abilityMod: 0);

        Assert.True(resolved[0].IsHealing);
        Assert.False(resolved[0].IsDamage);
    }

    [Fact]
    public void ResolveDamage_NoRecognizedKind_DefaultsToDamage()
    {
        var damage = new Pf2eSpellAutomation.SpellDamage([
            new Pf2eSpellAutomation.DamageInstance("1d6", "fire", [], ApplyMod: false)
        ]);

        var resolved = Pf2eSpellAutomation.ResolveDamage(damage, null, baseLevel: 1, castLevel: 1, abilityMod: 0);

        Assert.True(resolved[0].IsDamage);
        Assert.False(resolved[0].IsHealing);
    }

    [Fact]
    public void ResolveDamage_MultipleInstances_HeightenIncrementsAlignByIndex()
    {
        var damage = new Pf2eSpellAutomation.SpellDamage([
            new Pf2eSpellAutomation.DamageInstance("2d6", "fire", ["damage"], ApplyMod: false),
            new Pf2eSpellAutomation.DamageInstance("1d4", "persistent", ["damage"], ApplyMod: false),
        ]);
        var heightening = new Pf2eSpellAutomation.SpellHeightening(1, ["1d6", "1d4"]);

        var resolved = Pf2eSpellAutomation.ResolveDamage(damage, heightening, baseLevel: 1, castLevel: 2, abilityMod: 0);

        Assert.Equal("3d6", resolved[0].Expression);
        Assert.Equal("2d4", resolved[1].Expression);
    }
}
