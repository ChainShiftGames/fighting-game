using FightingGame.Presentation;
using Godot;
using Sim.Collision;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class StageCameraTests
{
    // 2 screen pixels per world pixel.
    private static readonly Vector2 _viewport = new(1152f, 648f);
    private static readonly StageWalls _walls = new(-600_000, 600_000);

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
        StageCamera small = StageCamera.Fit(_viewport, _walls, focusX: 0);
        StageCamera large = StageCamera.Fit(_viewport * 2f, _walls, focusX: 0);

        Assert.Equal(2f * small.PixelsPerSubpixel, large.PixelsPerSubpixel, 6);
        Assert.Equal(576f, small.FloorCentre.X);
    }

    [Fact]
    public void PutsTheFocusInTheMiddleOfTheScreen()
    {
        StageCamera camera = StageCamera.Fit(_viewport, _walls, focusX: 100_000);

        Assert.Equal(576f, camera.ToScreen(new IntVector2(100_000, 0)).X, 3);
    }

    [Fact]
    public void StopsWhenAWallReachesTheScreenEdge()
    {
        StageCamera right = StageCamera.Fit(_viewport, _walls, focusX: 590_000);
        StageCamera left = StageCamera.Fit(_viewport, _walls, focusX: -590_000);

        // 16 world pixels beyond the wall show, at 2 screen pixels each.
        Assert.Equal(_viewport.X - 32f, right.ToScreen(new IntVector2(_walls.Right, 0)).X, 3);
        Assert.Equal(32f, left.ToScreen(new IntVector2(_walls.Left, 0)).X, 3);
    }

    [Fact]
    public void CentresAStageNarrowerThanTheView()
    {
        StageCamera camera = StageCamera.Fit(_viewport, new StageWalls(-100_000, 100_000), focusX: 90_000);

        Assert.Equal(576f, camera.FloorCentre.X);
    }
}
