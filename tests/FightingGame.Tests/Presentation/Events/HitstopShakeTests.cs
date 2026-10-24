using FightingGame.Debug;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
using Godot;
using Sim.Fighters.States;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class HitstopShakeTests
{
    private readonly DeveloperToggle _toggle = new("Screen shake", Key.F8, enabled: true);

    [Fact]
    public void ADefenderInHitstopFlipsSidewaysEveryFrame()
    {
        var shake = new HitstopShake(TestEffectSettings.Standard.Shake, _toggle);

        IntVector2 first = shake.Offset(Pose(matchFrame: 10, stun: 12, hitstop: 8));
        IntVector2 second = shake.Offset(Pose(matchFrame: 11, stun: 12, hitstop: 7));

        Assert.Equal(new IntVector2(3 * Subpixels.PerPixel, 0), first);
        Assert.Equal(new IntVector2(-3 * Subpixels.PerPixel, 0), second);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(12, 0)]
    public void OnlyAFighterInStunAndHitstopShakes(int stun, int hitstop)
    {
        var shake = new HitstopShake(TestEffectSettings.Standard.Shake, _toggle);

        Assert.Equal(default, shake.Offset(Pose(matchFrame: 10, stun, hitstop)));
    }

    [Fact]
    public void TheScreenShakeSwitchTurnsItOff()
    {
        var shake = new HitstopShake(TestEffectSettings.Standard.Shake, _toggle);
        _toggle.Flip();

        Assert.Equal(default, shake.Offset(Pose(matchFrame: 10, stun: 12, hitstop: 8)));
    }

    private static FighterPose Pose(int matchFrame, int stun, int hitstop) =>
        new(FighterAction.Hitstun, "hitstun", 1, 1, MovePhase.Startup)
        {
            MatchFrame = matchFrame,
            StunFramesLeft = stun,
            HitstopFramesLeft = hitstop,
        };
}
