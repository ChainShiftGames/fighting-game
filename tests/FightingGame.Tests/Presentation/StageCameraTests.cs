using FightingGame.Presentation;
using Godot;
using Sim.Collision;
using Sim.Math;
using Xunit;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Tests.Presentation;

public sealed class StageCameraTests
{
    // 2 screen pixels per world pixel (228 world pixels high); wide enough for the 380 px logic screen.
    private static readonly Vector2 _viewport = new(912f, 456f);
    private static readonly StageWalls _walls = new(-600_000, 600_000);
    private static readonly LogicScreen _screen = new(190_000, _walls);

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
        Assert.Equal(456f, small.FloorCentre.X);
    }

    [Fact]
    public void PutsTheFocusInTheMiddleOfTheScreen()
    {
        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 100_000);

        Assert.Equal(456f, camera.ToScreen(new IntVector2(100_000, 0)).X, 3);
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

        Assert.Equal(456f, camera.FloorCentre.X);
    }

    [Fact]
    public void TheGroundViewIs228WorldPixelsWithTheFloor12PercentUp()
    {
        VerticalSpan ground = StageCamera.GroundView(_viewport, _screen);

        Assert.Equal(new VerticalSpan(-27_360, 200_640), ground);
    }

    [Fact]
    public void ShowingTheGroundViewFitsAsTheRoundStartCamera()
    {
        StageCamera roundStart = StageCamera.Fit(_viewport, _screen, focusX: 50_000);

        StageCamera camera = StageCamera.Fit(_viewport, _screen, 50_000, StageCamera.GroundView(_viewport, _screen));

        Assert.Equal(roundStart.PixelsPerSubpixel, camera.PixelsPerSubpixel, 6);
        Assert.Equal(roundStart.FloorCentre.X, camera.FloorCentre.X, 3);
        Assert.Equal(roundStart.FloorCentre.Y, camera.FloorCentre.Y, 3);
    }

    [Fact]
    public void ARaisedViewMovesTheFloorDownTheScreen()
    {
        var raised = new VerticalSpan(100_000, 328_000);

        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 0, raised);

        Assert.Equal(_viewport.Y, camera.ToScreen(new IntVector2(0, raised.Bottom)).Y, 3);
        Assert.Equal(0f, camera.ToScreen(new IntVector2(0, raised.Top)).Y, 3);
        Assert.True(camera.ToScreen(new IntVector2(0, 0)).Y > _viewport.Y, "the floor is below the screen");
    }

    [Fact]
    public void ATallerViewZoomsOutFromTheBottomOfTheScreenToTheTop()
    {
        var tall = new VerticalSpan(-8_000, 448_000);

        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 0, tall);

        Assert.Equal(_viewport.Y / 456_000f, camera.PixelsPerSubpixel, 6);
        Assert.Equal(_viewport.Y, camera.ToScreen(new IntVector2(0, tall.Bottom)).Y, 3);
        Assert.Equal(0f, camera.ToScreen(new IntVector2(0, tall.Top)).Y, 3);
    }

    [Fact]
    public void AShorterViewNeverZoomsIn()
    {
        StageCamera ground = StageCamera.Fit(_viewport, _screen, focusX: 0);

        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 0, new VerticalSpan(0, 100_000));

        Assert.Equal(ground.PixelsPerSubpixel, camera.PixelsPerSubpixel, 6);
    }

    [Fact]
    public void ZoomedOutItStillStopsWhenAWallReachesTheScreenEdge()
    {
        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 590_000, new VerticalSpan(0, 456_000));

        // 16 world pixels beyond the wall show, now at 1 screen pixel each.
        Assert.Equal(_viewport.X - 16f, camera.ToScreen(new IntVector2(_walls.Right, 0)).X, 3);
    }

    [Fact]
    public void ZoomsOutWhenTheViewportIsTooNarrowForTheLogicScreen()
    {
        var narrow = new Vector2(700f, 456f);

        StageCamera camera = StageCamera.Fit(narrow, _screen, focusX: 0);

        // The 380 px logic screen plus 12 px each side fills the width exactly.
        float left = camera.ToScreen(new IntVector2(-_screen.HalfWidth - 12_000, 0)).X;
        float right = camera.ToScreen(new IntVector2(_screen.HalfWidth + 12_000, 0)).X;
        Assert.Equal(0f, left, 3);
        Assert.Equal(narrow.X, right, 3);
    }
}
