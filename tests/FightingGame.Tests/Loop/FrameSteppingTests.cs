using FightingGame.Debug;
using FightingGame.Loop;
using Godot;
using Xunit;

namespace FightingGame.Tests.Loop;

public sealed class FrameSteppingTests
{
    private readonly DeveloperToggle _pause = new("Pause", Key.F5);
    private readonly FrameStepping _stepping;

    public FrameSteppingTests()
    {
        _stepping = new FrameStepping(_pause);
    }

    [Fact]
    public void RunsEveryDueFrameWhileNotPaused()
    {
        Assert.Equal(3, _stepping.Allow(3));
    }

    [Fact]
    public void RunsOnlyRequestedStepsWhilePaused()
    {
        _pause.Enabled = true;

        Assert.Equal(0, _stepping.Allow(2));
        _stepping.StepOnce();
        _stepping.StepOnce();
        Assert.Equal(2, _stepping.Allow(0));
        Assert.Equal(0, _stepping.Allow(1));
    }

    [Fact]
    public void IgnoresStepsWhileRunningSoResumingDoesNotJump()
    {
        _stepping.StepOnce();
        _pause.Enabled = true;

        Assert.Equal(0, _stepping.Allow(1));
    }

    [Fact]
    public void DropsUnusedStepsWhenResumed()
    {
        _pause.Enabled = true;
        _stepping.StepOnce();
        _pause.Enabled = false;

        Assert.Equal(1, _stepping.Allow(1));
        _pause.Enabled = true;
        Assert.Equal(0, _stepping.Allow(1));
    }
}
