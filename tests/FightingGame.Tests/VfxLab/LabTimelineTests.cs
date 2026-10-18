using FightingGame.VfxLab;
using FightingGame.VfxLab.Files;
using Xunit;

namespace FightingGame.Tests.VfxLab;

public sealed class LabTimelineTests
{
    private static readonly LabTiming _timing = new(ChargeFrames: 12, FlightFrames: 60, ImpactFrames: 30, RestFrames: 20);

    [Theory]
    [InlineData(0, LabPhase.Charge, 1)]
    [InlineData(11, LabPhase.Charge, 12)]
    [InlineData(12, LabPhase.Flight, 1)]
    [InlineData(71, LabPhase.Flight, 60)]
    [InlineData(72, LabPhase.Impact, 1)]
    [InlineData(101, LabPhase.Impact, 30)]
    [InlineData(102, LabPhase.Rest, 1)]
    [InlineData(122, LabPhase.Charge, 1)]
    [InlineData(-1, LabPhase.Rest, 20)]
    public void PlaysChargeFlightImpactAndRestInALoop(int frame, LabPhase phase, int phaseFrame)
    {
        LabMoment moment = LabTimeline.At(frame, _timing, LabPhaseLoop.Whole);

        Assert.Equal(phase, moment.Phase);
        Assert.Equal(phaseFrame, moment.PhaseFrame);
    }

    [Fact]
    public void LoopsOnePhaseOnItsOwn()
    {
        Assert.Equal(30, LabTimeline.Length(_timing, LabPhaseLoop.Impact));
        Assert.Equal(new LabMoment(LabPhase.Impact, 6, 5), LabTimeline.At(35, _timing, LabPhaseLoop.Impact));
    }

    [Fact]
    public void FlightEndsWhereTheTargetStands()
    {
        LabTiming timing = new(SpawnX: 45, Speed: 6, FlightFrames: 60, HitboxSize: 30, FighterWidth: 55);

        Assert.Equal(45f, LabLayout.FlightX(timing, 1));
        Assert.Equal(399f, LabLayout.ImpactX(timing));
        Assert.Equal(399f + 15f + 27.5f, LabLayout.TargetX(timing));
    }

    [Fact]
    public void TheMatchShotHoldsCasterAndTarget()
    {
        LabShot shot = LabShot.Of(_timing, default, LabFramingKind.Match, 1f, 16f / 9f);
        float halfWidth = shot.VisibleHeight * 16f / 9f / 2f;

        Assert.True(shot.CentreX - halfWidth <= -_timing.FighterWidth / 2f);
        Assert.True(shot.CentreX + halfWidth >= LabLayout.TargetX(_timing) + (_timing.FighterWidth / 2f));
    }

    [Fact]
    public void TheClockMovesInWholeFramesAtTheChosenSpeed()
    {
        LabClock clock = new() { Speed = 0.5 };

        clock.Advance(0.05);
        clock.Advance(0.05);

        Assert.Equal(3, clock.Frame);
        clock.Step(-2);
        Assert.False(clock.Playing);
        Assert.Equal(1, clock.Frame);
    }
}
