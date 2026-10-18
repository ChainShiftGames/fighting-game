using System;
using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Godot;
using Sim.Collision;
using Sim.Math;
using Xunit;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Tests.Presentation.World;

public sealed class CameraFramingTests
{
    private static readonly Vector2 _viewport = new(1152f, 648f);
    private static readonly LogicScreen _screen = new(220_000, new StageWalls(-600_000, 600_000));

    [Fact]
    public void ShowsThePlanePointThe2DLayersDrawAtTheScreenCentre()
    {
        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 140_000);

        CameraFraming framing = CameraFraming.Of(camera, _viewport);

        Vector2 drawnAt = camera.ToScreen(new IntVector2(
            (int)MathF.Round(framing.Centre.X * WorldScale.SubpixelsPerMetre),
            (int)MathF.Round(framing.Centre.Y * WorldScale.SubpixelsPerMetre)));
        Assert.Equal(_viewport.X / 2f, drawnAt.X, 1);
        Assert.Equal(_viewport.Y / 2f, drawnAt.Y, 1);
    }

    [Fact]
    public void ShowsAsMuchOfThePlaneAsTheStageCameraFromTopToBottom()
    {
        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 0);

        CameraFraming framing = CameraFraming.Of(camera, _viewport);

        float drawnHeight = WorldScale.ToMetres(_viewport.Y / camera.PixelsPerSubpixel);
        Assert.Equal(drawnHeight, framing.VisibleHeight, 4);
    }

    [Fact]
    public void MovesWithTheShake()
    {
        StageCamera camera = StageCamera.Fit(_viewport, _screen, focusX: 0);
        StageCamera shaken = camera with { FloorCentre = camera.FloorCentre + new Vector2(10f, 0f) };

        float still = CameraFraming.Of(camera, _viewport).Centre.X;
        float moved = CameraFraming.Of(shaken, _viewport).Centre.X;

        // The world moves right on screen, so the camera looks further left.
        Assert.True(moved < still);
    }

    [Fact]
    public void PlacesAPerspectiveCameraSoItsViewSpansTheVisibleHeight()
    {
        var framing = new CameraFraming(Vector3.Zero, VisibleHeight: 4f);

        float distance = framing.PerspectiveDistance(fieldOfViewDegrees: 90f);

        Assert.Equal(2f, distance, 4);
    }
}
