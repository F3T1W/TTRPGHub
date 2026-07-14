using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class InitiativeEntryTests
{
    [Fact]
    public void DeriveStatus_PositiveHpNoConditions_IsActive()
    {
        var status = InitiativeEntry.DeriveStatus(10, conditionsJson: null);

        Assert.Equal(EntryStatus.Active, status);
    }

    [Fact]
    public void DeriveStatus_ZeroHp_IsUnconscious()
    {
        var status = InitiativeEntry.DeriveStatus(0, conditionsJson: null);

        Assert.Equal(EntryStatus.Unconscious, status);
    }

    [Fact]
    public void DeriveStatus_DyingConditionAtPositiveHp_IsUnconscious()
    {
        var json = """[{"slug":"dying","name":"Dying","value":1}]""";

        var status = InitiativeEntry.DeriveStatus(5, json);

        Assert.Equal(EntryStatus.Unconscious, status);
    }

    [Fact]
    public void DeriveStatus_DeadConditionWinsOverPositiveHp()
    {
        var json = """[{"slug":"dead","name":"Dead","value":null}]""";

        var status = InitiativeEntry.DeriveStatus(5, json);

        Assert.Equal(EntryStatus.Dead, status);
    }

    [Fact]
    public void DeriveStatus_MalformedJson_FallsBackToHpOnly()
    {
        var status = InitiativeEntry.DeriveStatus(10, conditionsJson: "not json");

        Assert.Equal(EntryStatus.Active, status);
    }
}
