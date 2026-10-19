using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Godot;
using Sim.Rules;
using Xunit;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Tests.Presentation.World;

/// <summary>
/// The key art projection's defaults are the camera guide's frame: the round-start perspective camera at 16:9
/// (ADR-0030). When this fails, the camera settings changed and every stage's key art was painted for the old frame.
/// </summary>
public sealed class StageProjectionTests
{
    private static readonly Vector2 _guideViewport = new(1920f, 1080f);
    private static readonly string _shader = File.ReadAllText(Path.Combine(
        TestGameData.RepositoryRoot, "game", "Assets", "Shaders", "stage_projection.gdshader"));

    private static CameraFraming RoundStart()
    {
        MatchRules rules = TestGameData.Data.CreateMatchRules();
        StageCamera camera = StageCamera.Fit(_guideViewport, LogicScreen.From(in rules), focusX: 0);
        return CameraFraming.Of(camera, _guideViewport);
    }

    [Fact]
    public void ProjectsFromTheRoundStartCamera()
    {
        CameraFraming framing = RoundStart();

        float[] projector = Floats(Default("vec3 projector_position", @"vec3\(([^)]*)\)"));

        Assert.Equal(framing.Centre.X, projector[0], 3);
        Assert.Equal(framing.Centre.Y, projector[1], 3);
        Assert.Equal(framing.PerspectiveDistance(MatchCamera.PerspectiveFieldOfView), projector[2], 3);
    }

    [Fact]
    public void FramesThePaintingAsTheCameraGuideShowsThePlane()
    {
        CameraFraming framing = RoundStart();

        Assert.Equal(framing.Centre.X, Float(Default("float art_centre_x", "(.+)")), 3);
        Assert.Equal(framing.Centre.Y - (framing.VisibleHeight / 2f), Float(Default("float art_bottom", "(.+)")), 3);
        Assert.Equal(framing.VisibleHeight, Float(Default("float art_height", "(.+)")), 3);
    }

    private static string Default(string uniform, string value)
    {
        Match match = Regex.Match(_shader, $@"uniform {Regex.Escape(uniform)} = {value};");
        Assert.True(match.Success, $"no default for {uniform}");
        return match.Groups[1].Value;
    }

    private static float Float(string text) => float.Parse(text, CultureInfo.InvariantCulture);

    private static float[] Floats(string text) =>
        System.Array.ConvertAll(text.Split(','), part => Float(part.Trim()));
}
