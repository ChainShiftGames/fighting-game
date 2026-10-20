using FightingGame.Presentation.Events;
using Sim.Events;
using Sim.Math;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class HitSparksTests
{
    private readonly HitSparks _sparks = new();
    private FighterStates _fighters;

    public HitSparksTests()
    {
        _fighters[0].Facing = 1;
        _fighters[1].Facing = -1;
    }

    [Fact]
    public void ShowsASparkForItsLifetimeInLogicFrames()
    {
        _sparks.Play(Event(10, FrameEventKind.Hit));

        Assert.Equal(1, _sparks.Live(10, in _fighters).Length);
        Assert.Equal(1, _sparks.Live(10 + HitSparks.LifetimeFrames - 1, in _fighters).Length);
        Assert.Equal(0, _sparks.Live(10 + HitSparks.LifetimeFrames, in _fighters).Length);
    }

    [Fact]
    public void ACancelledSparkDisappearsAtOnce()
    {
        FrameEvent hit = Event(10, FrameEventKind.Hit);
        _sparks.Play(hit);

        _sparks.Cancel(hit);

        Assert.Equal(0, _sparks.Live(10, in _fighters).Length);
    }

    [Fact]
    public void CancellingOneSparkKeepsTheOthers()
    {
        FrameEvent first = Event(10, FrameEventKind.Hit);
        FrameEvent second = Event(11, FrameEventKind.Block);
        _sparks.Play(first);
        _sparks.Play(second);

        _sparks.Cancel(first);

        Assert.Equal(second, Assert.Single(_sparks.Live(11, in _fighters).ToArray()).Event);
    }

    [Fact]
    public void EventsWithoutContactHaveNoSpark()
    {
        _sparks.Play(Event(10, FrameEventKind.Swing));
        _sparks.Play(Event(10, FrameEventKind.Landing));

        Assert.Equal(0, _sparks.Live(10, in _fighters).Length);
    }

    [Fact]
    public void ACounterHitMarksItsHitsSparkInsteadOfAddingOne()
    {
        _sparks.Play(Event(10, FrameEventKind.Hit));
        _sparks.Play(Event(10, FrameEventKind.CounterHit));

        LiveSpark spark = Assert.Single(_sparks.Live(10, in _fighters).ToArray());
        Assert.Equal(FrameEventKind.CounterHit, spark.Counter);
    }

    [Fact]
    public void ACancelledCounterHitLeavesAPlainHit()
    {
        FrameEvent counter = Event(10, FrameEventKind.PunishCounter);
        _sparks.Play(Event(10, FrameEventKind.Hit));
        _sparks.Play(counter);

        _sparks.Cancel(counter);

        Assert.Equal(FrameEventKind.None, Assert.Single(_sparks.Live(10, in _fighters).ToArray()).Counter);
    }

    [Fact]
    public void AgesByLogicFrame()
    {
        _sparks.Play(Event(10, FrameEventKind.Hit));
        _sparks.Live(10, in _fighters);

        Assert.Equal(5, _sparks.Live(15, in _fighters)[0].ShownAge);
    }

    [Fact]
    public void ASparkFoundLateStillShowsItsFirstDrawingOnce()
    {
        _sparks.Play(Event(10, FrameEventKind.Hit));

        Assert.Equal(0, _sparks.Live(13, in _fighters)[0].ShownAge);
        Assert.Equal(4, _sparks.Live(14, in _fighters)[0].ShownAge);
    }

    [Fact]
    public void KeepsTheAttackersFacingFromItsFirstShowing()
    {
        _sparks.Play(Event(10, FrameEventKind.Hit, fighter: 1));
        _sparks.Live(10, in _fighters);

        _fighters[1].Facing = 1;

        Assert.Equal(-1, _sparks.Live(11, in _fighters)[0].Facing);
    }

    private static FrameEvent Event(int frame, FrameEventKind kind, int fighter = 0) =>
        new(frame, kind, fighter, occurrence: 0, new IntVector2(1_000, 50_000), detail: 1);
}
