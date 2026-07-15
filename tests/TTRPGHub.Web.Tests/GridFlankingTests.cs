using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class GridFlankingTests
{
    [Fact]
    public void IsFlanked_AttackersOnOppositeSides_ReturnsTrue()
    {
        var result = GridFlanking.IsFlanked(
            ax: 0, ay: 1, aW: 1, aH: 1,
            bx: 2, by: 1, bW: 1, bH: 1,
            tx: 1, ty: 1, tW: 1, tH: 1);

        Assert.True(result);
    }

    [Fact]
    public void IsFlanked_AttackersOnSameSide_ReturnsFalse()
    {
        var result = GridFlanking.IsFlanked(
            ax: 0, ay: 1, aW: 1, aH: 1,
            bx: 0, by: 0, bW: 1, bH: 1,
            tx: 1, ty: 1, tW: 1, tH: 1);

        Assert.False(result);
    }

    [Fact]
    public void IsFlanked_AttackerNotAdjacentToTarget_ReturnsFalse()
    {
        var result = GridFlanking.IsFlanked(
            ax: 5, ay: 5, aW: 1, aH: 1,
            bx: 2, by: 1, bW: 1, bH: 1,
            tx: 1, ty: 1, tW: 1, tH: 1);

        Assert.False(result);
    }

    [Fact]
    public void IsFlanked_BothAttackersSameSquare_ReturnsFalse()
    {
        var result = GridFlanking.IsFlanked(
            ax: 0, ay: 1, aW: 1, aH: 1,
            bx: 0, by: 1, bW: 1, bH: 1,
            tx: 1, ty: 1, tW: 1, tH: 1);

        Assert.False(result);
    }

    [Fact]
    public void IsFlanked_LargeCreatureFlanksAcrossItsFootprint_ReturnsTrue()
    {
        // A 2x2 attacker occupying (0,0)-(1,1) is adjacent to a target at (2,0),
        // and a defender directly opposite at (3,0) should flank it.
        var result = GridFlanking.IsFlanked(
            ax: 0, ay: 0, aW: 2, aH: 2,
            bx: 3, by: 0, bW: 1, bH: 1,
            tx: 2, ty: 0, tW: 1, tH: 1);

        Assert.True(result);
    }
}
