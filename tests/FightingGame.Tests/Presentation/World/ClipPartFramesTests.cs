using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class ClipPartFramesTests
{
    // The part of the clip played: clip frames 10-50 (clipFrom 10).
    private static readonly AnimationClipTiming _part = new(40f, Loops: false, StartFrame: 10f);

    [Fact]
    public void CountsContactFramesAndKeysFromThePartsStart()
    {
        AnimationClipTiming timing = ClipPartFrames.Fit(
            _part, new ContactFrames(30, 36), [new TimingKey(MovePhase.Startup, 2, 4, 20)], out string problem);

        Assert.Equal(string.Empty, problem);
        Assert.Equal(new ContactFrames(20, 26), timing.Contacts);
        Assert.Equal([new TimingKey(MovePhase.Startup, 2, 4, 10)], timing.Keys!);
    }

    [Fact]
    public void LeavesOutOnlyTheKeysWhenAKeyIsOutsideThePart()
    {
        AnimationClipTiming timing = ClipPartFrames.Fit(
            _part, new ContactFrames(30, 36), [new TimingKey(MovePhase.Startup, 2, 4, 5)], out string problem);

        Assert.Contains("timing key 0", problem);
        Assert.Equal(new ContactFrames(20, 26), timing.Contacts);
        Assert.Null(timing.Keys);
    }

    [Fact]
    public void PlaysFrameForFrameWhenTheContactFramesAreOutsideThePart()
    {
        AnimationClipTiming timing = ClipPartFrames.Fit(
            _part, new ContactFrames(30, 60), [new TimingKey(MovePhase.Startup, 2, 4, 20)], out string problem);

        Assert.Contains("contact frames", problem);
        Assert.Null(timing.Contacts);
        Assert.Null(timing.Keys);
    }
}
