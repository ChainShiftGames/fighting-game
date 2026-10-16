using FightingGame.Presentation;
using Godot;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class StageCameraTests
{
    private readonly StageCamera _camera = new(new Vector2(500f, 400f), 0.002f);

    [Fact]
    public void PutsTheWorldOriginOnTheFloorCentreWithYUp()
    {
        Vector2 origin = _camera.ToScreen(new IntVector2(0, 0));
        Vector2 upRight = _camera.ToScreen(new IntVector2(100_000, 100_000));

        Assert.Equal(new Vector2(500f, 400f), origin);
        Assert.Equal(700f, upRight.X, 3);
        Assert.Equal(200f, upRight.Y, 3);
    }

    [Fact]
    public void DrawsABoxFromItsTopLeftCorner()
    {
        Rect2 rect = _camera.ToScreen(AxisAlignedBox.FromCorner(-10_000, 0, 20_000, 50_000));

        Assert.Equal(480f, rect.Position.X, 3);
        Assert.Equal(300f, rect.Position.Y, 3);
        Assert.Equal(40f, rect.Size.X, 3);
        Assert.Equal(100f, rect.Size.Y, 3);
    }

    [Fact]
    public void ScalesTheWorldToTheViewportHeight()
    {
        StageCamera small = StageCamera.Fit(new Vector2(1152f, 648f));
        StageCamera large = StageCamera.Fit(new Vector2(2304f, 1296f));

        Assert.Equal(2f * small.PixelsPerSubpixel, large.PixelsPerSubpixel, 6);
        Assert.Equal(576f, small.FloorCentre.X);
    }
}
