using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>The editor's model preview poses a move as the match does (ADR-0028).</summary>
public sealed class MovePoseTests
{
    [Theory]
    [InlineData(4, MovePhase.Startup)]
    [InlineData(5, MovePhase.Active)]
    [InlineData(8, MovePhase.Recovery)]
    public void PosesAnAttackFromItsFrameDataAsEdited(int frame, MovePhase phase)
    {
        JsonObject move = Attack(startup: 5, active: 3, recovery: 8);

        FighterPose pose = MovePose.Of(move, frame)!.Value;

        Assert.Equal("5a", pose.MoveId);
        Assert.Equal(15, pose.TotalFrames);
        Assert.Equal(5, pose.FirstActiveFrame);
        Assert.Equal(7, pose.LastActiveFrame);
        Assert.Equal(phase, pose.Phase);
    }

    [Fact]
    public void ShowsTheContactPoseOnTheFirstActiveFrameAfterStartupIsEdited()
    {
        // Startup 5 → 9: the model still hits on the first active frame, as in the match.
        FighterPose pose = MovePose.Of(Attack(startup: 9, active: 3, recovery: 8), 9)!.Value;
        var clip = new AnimationClipTiming(40f, Loops: false, new ContactFrames(10, 16));

        Assert.Equal(10f, AnimationRetiming.FrameAt(pose, clip), 4);
    }

    [Fact]
    public void PosesABasicMoveWithoutActiveFrames()
    {
        var idle = new JsonObject { ["id"] = "idle", ["duration"] = 60 };

        FighterPose pose = MovePose.Of(idle, 12)!.Value;

        Assert.Equal(0, pose.FirstActiveFrame);
        Assert.Equal(MovePhase.None, pose.Phase);
    }

    [Fact]
    public void HasNoPoseForAMoveWithoutReadableFrameData()
    {
        var broken = new JsonObject { ["id"] = "5a", ["frameData"] = new JsonObject { ["startup"] = "five" } };

        Assert.Null(MovePose.Of(broken, 1));
    }

    private static JsonObject Attack(int startup, int active, int recovery) => new()
    {
        ["id"] = "5a",
        ["frameData"] = new JsonObject { ["startup"] = startup, ["active"] = active, ["recovery"] = recovery },
    };
}
