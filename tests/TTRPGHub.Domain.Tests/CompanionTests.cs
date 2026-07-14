using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class CompanionTests
{
    [Fact]
    public void Create_BlankName_ReturnsValidationError()
    {
        var result = Companion.Create(CharacterId.New(), "   ", "Animal Companion", 1, 10, null, null, null, null, null);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_TrimsNameAndKind()
    {
        var result = Companion.Create(CharacterId.New(), "  Wolf  ", "  Familiar  ", 1, 10, null, null, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Wolf", result.Value!.Name);
        Assert.Equal("Familiar", result.Value.Kind);
    }

    [Fact]
    public void Create_BlankKind_DefaultsToCompanion()
    {
        var result = Companion.Create(CharacterId.New(), "Wolf", "  ", 1, 10, null, null, null, null, null);

        Assert.Equal("Компаньон", result.Value!.Kind);
    }

    [Fact]
    public void Create_NegativeMaxHitPoints_ClampsToZero()
    {
        var result = Companion.Create(CharacterId.New(), "Wolf", "Companion", 1, -5, null, null, null, null, null);

        Assert.Equal(0, result.Value!.MaxHitPoints);
    }

    [Fact]
    public void Create_StartsAtFullHealth()
    {
        var result = Companion.Create(CharacterId.New(), "Wolf", "Companion", 1, 20, null, null, null, null, null);

        Assert.Equal(20, result.Value!.CurrentHitPoints);
    }

    [Fact]
    public void Update_BlankName_ReturnsValidationErrorAndLeavesStateUnchanged()
    {
        var companion = Companion.Create(CharacterId.New(), "Wolf", "Companion", 1, 10, null, null, null, null, null).Value!;

        var result = companion.Update("  ", "Companion", 2, 14, 14, null, null, null, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Wolf", companion.Name);
        Assert.Equal(1, companion.Level);
    }

    [Fact]
    public void Update_CurrentHitPointsAboveNewMax_ClampsDown()
    {
        var companion = Companion.Create(CharacterId.New(), "Wolf", "Companion", 1, 20, null, null, null, null, null).Value!;

        companion.Update("Wolf", "Companion", 1, 10, 999, null, null, null, null, null);

        Assert.Equal(10, companion.CurrentHitPoints);
    }

    [Fact]
    public void Update_NegativeCurrentHitPoints_ClampsToZero()
    {
        var companion = Companion.Create(CharacterId.New(), "Wolf", "Companion", 1, 20, null, null, null, null, null).Value!;

        companion.Update("Wolf", "Companion", 1, 20, -50, null, null, null, null, null);

        Assert.Equal(0, companion.CurrentHitPoints);
    }
}
