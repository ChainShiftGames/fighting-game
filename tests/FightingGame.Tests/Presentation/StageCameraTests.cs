using FightingGame.Presentation;
using Godot;
using Sim.Collision;
using Sim.Math;
using Xunit;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Tests.Presentation;

public sealed class StageCameraTests
{
    // 2 screen pixels per world pixel (264 world pixels high); wide enough for the 440 px logic screen.
    private static readonly Vector2 _viewport = new(1056f, 528f);
    private static readonly StageWalls _walls = new(-600_000, 600_000);
    private static readonly LogicScreen _screen = new(220_000, _walls);

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
        StageCamera small = StageCamera.Fit(_viewport, _screen, focusX: 0);
        StageCamera large = StageCamera.Fit(_viewport * 2f, _screen, focusX: 0);

        Assert.Equal(2f * small.PixelsPerSubpixel, large.PixelsPerSubpixel, 6);
        Assert.Equal(528f, small.FloorCentre.X);
    }

    [Fact]
    public void PutsTheFocusInTheMiddleOfTheScreen()
    {
        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 100_000);

        Assert.Equal(528f, camera.ToScreen(new IntVector2(100_000, 0)).X, 3);
    }

    [Fact]
    public void StopsWhenAWallReachesTheScreenEdge()
    {
        StageCamera right = StageCamera.Fit(_viewport, _screen, focusX: 590_000);
        StageCamera left = StageCamera.Fit(_viewport, _screen, focusX: -590_000);

        // 16 world pixels beyond the wall show, at 2 screen pixels each.
        Assert.Equal(_viewport.X - 32f, right.ToScreen(new IntVector2(_walls.Right, 0)).X, 3);
        Assert.Equal(32f, left.ToScreen(new IntVector2(_walls.Left, 0)).X, 3);
    }

    [Fact]
    public void CentresAStageNarrowerThanTheView()
    {
        StageCamera camera = StageCamera.Fit(_viewport, new LogicScreen(100_000, new StageWalls(-100_000, 100_000)), focusX: 90_000);

        Assert.Equal(528f, camera.FloorCentre.X);
    }

    [Fact]
    public void ZoomsOutWhenTheViewportIsTooNarrowForTheLogicScreen()
    {
        var narrow = new Vector2(800f, 528f);

        StageCamera camera = StageCamera.Fit(narrow, _screen, focusX: 0);

        // The 440 px logic screen plus 12 px each side fills the width exactly.
        float left = camera.ToScreen(new IntVector2(-_screen.HalfWidth - 12_000, 0)).X;
        float right = camera.ToScreen(new IntVector2(_screen.HalfWidth + 12_000, 0)).X;
        Assert.Equal(0f, left, 3);
        Assert.Equal(narrow.X, right, 3);
    }
}
