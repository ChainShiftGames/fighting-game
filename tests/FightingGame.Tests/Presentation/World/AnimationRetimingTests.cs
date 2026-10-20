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

    [Theory]
    [InlineData(1, false, 0f)]
    [InlineData(5, false, 28f)]
    [InlineData(1, true, 56f)]
    [InlineData(8, true, 7f)]
    public void SqueezesATransitionClipIntoItsFrames(int frame, bool reverse, float clipFrame)
    {
        // A 56-frame clip in 8 logic frames: 7 clip frames each; reversed it starts from the end.
        Assert.Equal(clipFrame, AnimationRetiming.TransitionFrameAt(frame, 8, 56f, reverse), 4);
    }

    [Fact]
    public void PlaysOnlyThePartOfTheClipBetweenClipFromAndClipTo()
    {
        // A jump clip's airborne part, clip frames 28-66, from takeoff.
        var airborne = new AnimationClipTiming(38f, Loops: false, StartFrame: 28f);
        var jump = new FighterPose(FighterAction.Jump, "jump", 11, 1, MovePhase.None);

        Assert.Equal(38f, AnimationRetiming.FrameAt(jump, airborne), 4);
        Assert.Equal(66f, AnimationRetiming.FrameAt(jump with { Frame = 60 }, airborne), 4);
    }

    [Theory]
    [InlineData(1, 8f)]
    [InlineData(3, 18f)]
    [InlineData(4, 23f)]
    public void FitsAPartOfTheClipIntoTheMove(int frame, float clipFrame)
    {
        // Prejump: the clip's wind-up, frames 8-28, squeezed into 4 frames.
        var windUp = new AnimationClipTiming(20f, Loops: false, StartFrame: 8f, Fit: true);
        var prejump = new FighterPose(FighterAction.Prejump, "prejump", frame, 4, MovePhase.None);

        Assert.Equal(clipFrame, AnimationRetiming.FrameAt(prejump, windUp), 4);
    }

    [Theory]
    [InlineData(1, 0f)]
    [InlineData(2, 0f)]
    [InlineData(3, 2f)]
    [InlineData(6, 4f)]
    public void HoldsEachPoseForItsHoldFrames(int frame, float clipFrame)
    {
        var onTwos = new AnimationClipTiming(30f, Loops: false, HoldFrames: 2);

        Assert.Equal(clipFrame, AnimationRetiming.FrameAt(Attack(frame) with { FirstActiveFrame = 0 }, onTwos), 4);
    }

    [Theory]
    [InlineData(5, 7)]
    [InlineData(10, 13)]
    public void ShowsTheContactPoseOnTheFirstActiveFrameOnTwosWhateverTheStartup(int firstActive, int lastActive)
    {
        // Odd (5a) and even (5c) startups: each phase holds from its own first frame.
        var move = new FighterPose(FighterAction.Attack, "5c", firstActive, 34, MovePhase.Active, firstActive, lastActive);
        AnimationClipTiming onTwos = Retimed(activeFrom: 10, recoveryFrom: 16, length: 40f) with { HoldFrames = 2 };

        Assert.Equal(10f, AnimationRetiming.FrameAt(move, onTwos), 4);
        Assert.Equal(10f, AnimationRetiming.FrameAt(move with { Frame = firstActive + 1 }, onTwos), 4);
        Assert.Equal(16f, AnimationRetiming.FrameAt(move with { Frame = lastActive + 1 }, onTwos), 4);
    }

    [Fact]
    public void HoldsTheLastStartupPoseOnlyUntilTheFirstActiveFrame()
    {
        // 9 startup frames before 5c's first active frame 10, over clip frames 0-10: frame 9 holds its own pose.
        var move = new FighterPose(FighterAction.Attack, "5c", 9, 34, MovePhase.Startup, 10, 13);
        AnimationClipTiming onTwos = Retimed(activeFrom: 10, recoveryFrom: 16, length: 40f) with { HoldFrames = 2 };

        Assert.Equal(80f / 9f, AnimationRetiming.FrameAt(move, onTwos), 4);
        Assert.Equal(0f, AnimationRetiming.FrameAt(move with { Frame = 2 }, onTwos), 4);
    }

    [Fact]
    public void ShowsTheFirstKeyOnTheFrameAMoveIsCancelledInto()
    {
        AnimationClipTiming onTwos = Retimed(activeFrom: 10, recoveryFrom: 16, length: 40f) with { HoldFrames = 2 };

        Assert.Equal(0f, AnimationRetiming.FrameAt(Attack(1), onTwos), 4);
    }

    [Theory]
    [InlineData(35, 4f)]
    [InlineData(36, 4f)]
    [InlineData(37, 6f)]
    public void HoldsALoopOnTwosFromItsFirstFrame(int frame, float clipFrame)
    {
        // Idle and walk have no phases: held from frame 1 as before, wrapping at the loop's end.
        var walk = new FighterPose(FighterAction.WalkForward, "walk_forward", frame, 60, MovePhase.None);

        float shown = AnimationRetiming.FrameAt(walk, new AnimationClipTiming(30f, Loops: true, HoldFrames: 2));

        Assert.Equal(clipFrame, shown, 4);
    }

    [Fact]
    public void PlaysAClipFasterAtItsSpeed()
    {
        // A walk cycle sped up to the logic's walk speed: frame 11 is 10 frames in, 26 clip frames at 2.6.
        var walk = new FighterPose(FighterAction.WalkForward, "walk_forward", 11, 60, MovePhase.None);

        float shown = AnimationRetiming.FrameAt(walk, new AnimationClipTiming(58f, Loops: true, Speed: 2.6f));

        Assert.Equal(26f, shown, 3);
    }

    private static FighterPose Attack(int frame) =>
        new(FighterAction.Attack, "5a", frame, Total, MovePhase.Startup, FirstActive, LastActive);

    private static AnimationClipTiming Retimed(int activeFrom, int recoveryFrom, float length) =>
        new(length, Loops: false, new ContactFrames(activeFrom, recoveryFrom));
}
