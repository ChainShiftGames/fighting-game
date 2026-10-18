using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class AnimationRetimingTests
{
    // 5A's frame data: startup 5 (first active frame 5), active 5-7, recovery 8-15.
    private const int FirstActive = 5;
    private const int LastActive = 7;
    private const int Total = 15;

    [Theory]
    [InlineData(1, 0f)]
    [InlineData(2, 1f / 60f)]
    [InlineData(15, 14f / 60f)]
    public void PlaysAClipWithoutContactFramesFrameForFrame(int frame, float seconds)
    {
        double time = AnimationRetiming.SecondsAt(Attack(frame), new AnimationClipTiming(30f, Loops: false));

        Assert.Equal(seconds, time, 5);
    }

    [Theory]
    [InlineData(1, 0f)]
    [InlineData(3, 5f)]
    [InlineData(4, 7.5f)]
    public void StretchesStartupOverTheClipUpToTheActivePose(int frame, float clipFrame)
    {
        // 4 startup frames before the first active one, shown over clip frames 0-10.
        float shown = AnimationRetiming.FrameAt(Attack(frame), Retimed(activeFrom: 10, recoveryFrom: 16, length: 40f));

        Assert.Equal(clipFrame, shown, 4);
    }

    [Theory]
    [InlineData(5, 10f)]
    [InlineData(6, 12f)]
    [InlineData(7, 14f)]
    public void ShowsTheContactPoseOnTheFirstActiveFrame(int frame, float clipFrame)
    {
        float shown = AnimationRetiming.FrameAt(Attack(frame), Retimed(activeFrom: 10, recoveryFrom: 16, length: 40f));

        Assert.Equal(clipFrame, shown, 4);
    }

    [Theory]
    [InlineData(8, 16f)]
    [InlineData(12, 28f)]
    [InlineData(15, 37f)]
    public void StretchesRecoveryOverTheRestOfTheClip(int frame, float clipFrame)
    {
        // 8 recovery frames over clip frames 16-40: 3 clip frames each.
        float shown = AnimationRetiming.FrameAt(Attack(frame), Retimed(activeFrom: 10, recoveryFrom: 16, length: 40f));

        Assert.Equal(clipFrame, shown, 4);
    }

    [Fact]
    public void KeepsTheContactPoseWhenTheFrameDataChanges()
    {
        // Startup 9 instead of 5: the first active frame still shows the contact pose.
        var slower = new FighterPose(FighterAction.Attack, "5a", 9, 19, MovePhase.Active, 9, 11);

        float shown = AnimationRetiming.FrameAt(slower, Retimed(activeFrom: 10, recoveryFrom: 16, length: 40f));

        Assert.Equal(10f, shown, 4);
    }

    [Fact]
    public void HoldsTheLastPoseOfAClipThatDoesNotLoop()
    {
        double time = AnimationRetiming.SecondsAt(Attack(15), new AnimationClipTiming(6f, Loops: false));

        Assert.Equal(6f / 60f, time, 5);
    }

    [Fact]
    public void WrapsAClipThatLoops()
    {
        var walk = new FighterPose(FighterAction.WalkForward, "walk_forward", 35, 60, MovePhase.None);

        float shown = AnimationRetiming.FrameAt(walk, new AnimationClipTiming(30f, Loops: true));

        Assert.Equal(4f, shown, 4);
    }

    [Fact]
    public void IgnoresContactFramesForAMoveWithoutActiveFrames()
    {
        var idle = new FighterPose(FighterAction.Idle, "idle", 11, 60, MovePhase.None);

        float shown = AnimationRetiming.FrameAt(idle, Retimed(activeFrom: 2, recoveryFrom: 4, length: 60f));

        Assert.Equal(10f, shown, 4);
    }

    private static FighterPose Attack(int frame) =>
        new(FighterAction.Attack, "5a", frame, Total, MovePhase.Startup, FirstActive, LastActive);

    private static AnimationClipTiming Retimed(int activeFrom, int recoveryFrom, float length) =>
        new(length, Loops: false, new ContactFrames(activeFrom, recoveryFrom));
}
