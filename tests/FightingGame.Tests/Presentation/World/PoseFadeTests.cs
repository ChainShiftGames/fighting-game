using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class PoseFadeTests
{
    [Fact]
    public void FadesABasicMoveInFromThePoseShownBefore()
    {
        var fade = new PoseFade();
        fade.Observe(clip: 1, seconds: 0.3, fadeFrames: 0, startsOver: false, matchFrame: 10);
        fade.Observe(clip: 2, seconds: 0, fadeFrames: 4, startsOver: false, matchFrame: 11);

        Assert.Equal(1, fade.FromClip);
        Assert.Equal(0.3, fade.FromSeconds, 5);
        Assert.Equal(0.2f, fade.Weight(11), 4);
        Assert.Equal(0.8f, fade.Weight(14), 4);
        Assert.Equal(1f, fade.Weight(15), 4);
    }

    [Fact]
    public void SnapsIntoAnAttack()
    {
        var fade = new PoseFade();
        fade.Observe(1, 0.3, fadeFrames: 0, startsOver: false, matchFrame: 10);
        fade.Observe(3, 0, fadeFrames: 0, startsOver: false, matchFrame: 11);

        Assert.Equal(-1, fade.FromClip);
        Assert.Equal(1f, fade.Weight(11));
    }

    [Fact]
    public void DropsTheFadeWhenTheMatchGoesBack()
    {
        var fade = new PoseFade();
        fade.Observe(1, 0.3, fadeFrames: 0, startsOver: false, matchFrame: 10);
        fade.Observe(2, 0, fadeFrames: 4, startsOver: false, matchFrame: 11);
        fade.Observe(2, 0, fadeFrames: 4, startsOver: false, matchFrame: 9);

        Assert.Equal(1f, fade.Weight(9));
    }

    [Fact]
    public void FadesAReactionThatStartsOverFromItsOwnPoseShownBefore()
    {
        // Hitstun shown 0.5 s in, then a re-hit restarts it at 0 s: the clip fades from its own earlier pose.
        var fade = new PoseFade();
        fade.Observe(clip: 5, seconds: 0.5, fadeFrames: 0, startsOver: false, matchFrame: 20);
        fade.Observe(clip: 5, seconds: 0, fadeFrames: 6, startsOver: true, matchFrame: 21);

        Assert.Equal(5, fade.FromClip);
        Assert.Equal(0.5, fade.FromSeconds, 5);
        Assert.Equal(1f / 7f, fade.Weight(21), 4);

        // Held by hitstop, the re-hit keeps easing in by the match clock with the fade length it started with.
        fade.Observe(clip: 5, seconds: 0, fadeFrames: 0, startsOver: false, matchFrame: 24);
        Assert.Equal(4f / 7f, fade.Weight(24), 4);
        Assert.Equal(1f, fade.Weight(27), 4);
    }

    [Fact]
    public void KeepsShowingTheSameClipWithoutAFadeWhenItDoesNotStartOver()
    {
        var fade = new PoseFade();
        fade.Observe(5, 0.5, fadeFrames: 0, startsOver: false, matchFrame: 20);
        fade.Observe(5, 0.6, fadeFrames: 6, startsOver: false, matchFrame: 21);

        Assert.Equal(-1, fade.FromClip);
    }

    [Theory]
    [InlineData(FighterAction.Hitstun)]
    [InlineData(FighterAction.CrouchHitstun)]
    [InlineData(FighterAction.AirHitstun)]
    [InlineData(FighterAction.Blockstun)]
    [InlineData(FighterAction.CrouchBlockstun)]
    [InlineData(FighterAction.AirBlockstun)]
    public void HitAndBlockReactionsSnapIn(FighterAction reaction)
    {
        Assert.False(PoseFade.MayFadeInto(new FighterPose(reaction, "hitstun", 1, 1, MovePhase.None)));
    }

    [Theory]
    [InlineData(FighterAction.Idle)]
    [InlineData(FighterAction.Crouch)]
    [InlineData(FighterAction.WalkForward)]
    [InlineData(FighterAction.WalkBack)]
    public void OtherBasicMovesMayFadeIn(FighterAction action)
    {
        Assert.True(PoseFade.MayFadeInto(new FighterPose(action, "idle", 1, 60, MovePhase.None)));
    }

    [Fact]
    public void AttacksSnapIn()
    {
        var attack = new FighterPose(FighterAction.Attack, "5a", 1, 15, MovePhase.Startup, 5, 7);

        Assert.False(PoseFade.MayFadeInto(attack));
    }
}
