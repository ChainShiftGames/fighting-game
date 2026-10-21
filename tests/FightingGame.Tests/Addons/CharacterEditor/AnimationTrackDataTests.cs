using System.Linq;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>The animation track shows the clip frames the match would show (ADR-0028).</summary>
public sealed class AnimationTrackDataTests
{
    // 5a: startup 5 (frames 1-4), active 5-7, recovery 8-15; the clip plays frames 10-50 (clipFrom 10).
    private static readonly JsonObject _move = new()
    {
        ["id"] = "5a",
        ["frameData"] = new JsonObject { ["startup"] = 5, ["active"] = 3, ["recovery"] = 8 },
    };

    private static readonly AnimationClipTiming _clip = new(40f, Loops: false, StartFrame: 10f);

    [Fact]
    public void ShowsEachFramesClipFrameWithTheKeysAndContactFrames()
    {
        var timing = new MoveAnimationTiming(new ContactFrames(20, 26), [new TimingKey(MovePhase.Startup, 3, 4, 18)]);

        AnimationTrackData data = AnimationTrackData.Of(_move, _clip, timing)!;

        Assert.Equal(15, data.ClipFrames.Count);
        Assert.Equal([10f, 14f, 18f, 19f, 20f], data.ClipFrames.Take(5));
        Assert.Equal(new ClipPart(10, 50), data.Part);
        Assert.Equal(new TrackPin(3, 18, TrackPinKind.Key), data.PinOn(3));
        Assert.Equal(TrackPinKind.ActiveFrom, data.PinOn(5)!.Value.Kind);
        Assert.Null(data.PinOn(4));
        Assert.Equal(19, data.ClipAt(4));
    }

    [Fact]
    public void HasNoTrackForABasicMove()
    {
        var idle = new JsonObject { ["id"] = "idle", ["duration"] = 60 };

        Assert.Null(AnimationTrackData.Of(idle, _clip, MoveAnimationTiming.None));
    }
}
