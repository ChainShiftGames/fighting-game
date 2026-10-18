using FightingGame.Presentation.World;
using Godot;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class WorldScaleTests
{
    [Fact]
    public void PutsLogicPositionsOnThePlaneInMetresWithYUp()
    {
        Vector3 world = WorldScale.ToWorld(new IntVector2(140_000, 35_000));

        Assert.Equal(2f, world.X, 4);
        Assert.Equal(0.5f, world.Y, 4);
        Assert.Equal(0f, world.Z);
    }

    [Fact]
    public void MakesTestmansStandingHurtboxAboutHumanHeight()
    {
        float height = WorldScale.ToMetres(120_000);

        Assert.InRange(height, 1.6f, 1.8f);
    }

    [Fact]
    public void CentresABoxOnTheMiddleOfItsArea()
    {
        Vector3 centre = WorldScale.CentreOf(AxisAlignedBox.FromCorner(-35_000, 0, 70_000, 140_000));

        Assert.Equal(0f, centre.X, 4);
        Assert.Equal(1f, centre.Y, 4);
    }
}
