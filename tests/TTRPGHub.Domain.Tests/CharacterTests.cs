using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class CharacterTests
{
    private static Character CreateValid(out UserId ownerId)
    {
        ownerId = UserId.New();
        var result = Character.Create(ownerId, "Aragorn", "Human", "Fighter", 1);
        Assert.True(result.IsSuccess);
        return result.Value!;
    }

    [Fact]
    public void Create_WithBlankName_Fails()
    {
        var result = Character.Create(UserId.New(), "  ", "Human", "Fighter", 1);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Create_WithLevelOutOfRange_Fails(int level)
    {
        var result = Character.Create(UserId.New(), "Aragorn", "Human", "Fighter", level);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_TrimsName()
    {
        var result = Character.Create(UserId.New(), "  Aragorn  ", "Human", "Fighter", 1);

        Assert.Equal("Aragorn", result.Value!.Name);
    }

    [Fact]
    public void IsOwnedBy_ReturnsTrueForOwner()
    {
        var character = CreateValid(out var ownerId);

        Assert.True(character.IsOwnedBy(ownerId));
    }

    [Fact]
    public void IsOwnedBy_ReturnsFalseForStranger()
    {
        var character = CreateValid(out _);

        Assert.False(character.IsOwnedBy(UserId.New()));
    }

    [Fact]
    public void AddCoOwner_ThenIsOwnedBy_ReturnsTrue()
    {
        var character = CreateValid(out _);
        var coOwnerId = Guid.NewGuid();

        var result = character.AddCoOwner(coOwnerId);

        Assert.True(result.IsSuccess);
        Assert.True(character.IsOwnedBy(new UserId(coOwnerId)));
    }

    [Fact]
    public void AddCoOwner_Owner_Fails()
    {
        var character = CreateValid(out var ownerId);

        var result = character.AddCoOwner(ownerId.Value);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddCoOwner_AlreadyCoOwner_Fails()
    {
        var character = CreateValid(out _);
        var coOwnerId = Guid.NewGuid();
        character.AddCoOwner(coOwnerId);

        var result = character.AddCoOwner(coOwnerId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RemoveCoOwner_RevokesOwnership()
    {
        var character = CreateValid(out _);
        var coOwnerId = Guid.NewGuid();
        character.AddCoOwner(coOwnerId);

        character.RemoveCoOwner(coOwnerId);

        Assert.False(character.IsOwnedBy(new UserId(coOwnerId)));
    }

    private static UpdateSheetData ValidSheet(int strength = 10, int maxHp = 20, int currentHp = 20) => new(
        Name: "Aragorn", Race: "Human", Class: "Fighter", Level: 1, IsPublic: false,
        Background: null, Alignment: null, ExperiencePoints: 0,
        PersonalityTraits: null, Ideals: null, Bonds: null, Flaws: null,
        Strength: strength, Dexterity: 10, Constitution: 10, Intelligence: 10, Wisdom: 10, Charisma: 10,
        MaxHitPoints: maxHp, CurrentHitPoints: currentHp, TemporaryHitPoints: 0,
        ArmorClass: 10, Speed: 30, HitDice: "1d10",
        SkillProficiencies: [], SavingThrowProficiencies: [],
        FeaturesAndTraits: null, Equipment: null);

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void UpdateSheet_ClampsAbilityScoresTo1Through30(int rawStrength)
    {
        var character = CreateValid(out _);

        character.UpdateSheet(ValidSheet(strength: rawStrength));

        Assert.InRange(character.Strength, 1, 30);
    }

    [Fact]
    public void UpdateSheet_ClampsCurrentHpToMax()
    {
        var character = CreateValid(out _);

        character.UpdateSheet(ValidSheet(maxHp: 10, currentHp: 999));

        Assert.Equal(10, character.CurrentHitPoints);
    }

    [Fact]
    public void UpdateSheet_NegativeArmorClass_Fails()
    {
        var character = CreateValid(out _);

        var result = character.UpdateSheet(ValidSheet() with { ArmorClass = -1 });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SetCurrentHitPoints_ClampsToMax()
    {
        var character = CreateValid(out _);
        character.UpdateSheet(ValidSheet(maxHp: 15, currentHp: 15));

        character.SetCurrentHitPoints(999);

        Assert.Equal(15, character.CurrentHitPoints);
    }

    [Fact]
    public void SetCurrentHitPoints_ClampsToZero()
    {
        var character = CreateValid(out _);
        character.UpdateSheet(ValidSheet(maxHp: 15, currentHp: 15));

        character.SetCurrentHitPoints(-5);

        Assert.Equal(0, character.CurrentHitPoints);
    }
}
