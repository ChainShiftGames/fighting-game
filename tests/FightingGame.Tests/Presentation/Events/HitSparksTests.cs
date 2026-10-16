using FightingGame.Presentation.Events;
using Sim.Events;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class HitSparksTests
{
    private readonly HitSparks _sparks = new();

    [Fact]
    public void ShowsASparkForItsLifetimeInLogicFrames()
    {
        _sparks.Play(Event(10, FrameEventKind.Hit));

        Assert.Equal(1, _sparks.Live(10).Length);
        Assert.Equal(1, _sparks.Live(10 + HitSparks.LifetimeFrames - 1).Length);
        Assert.Equal(0, _sparks.Live(10 + HitSparks.LifetimeFrames).Length);
    }

    [Fact]
    public void ACancelledSparkDisappearsAtOnce()
    {
        FrameEvent hit = Event(10, FrameEventKind.Hit);
        _sparks.Play(hit);

        _sparks.Cancel(hit);

        Assert.Equal(0, _sparks.Live(10).Length);
    }

    [Fact]
    public void CancellingOneSparkKeepsTheOthers()
    {
        FrameEvent first = Event(10, FrameEventKind.Hit);
        FrameEvent second = Event(11, FrameEventKind.Block);
        _sparks.Play(first);
        _sparks.Play(second);

        _sparks.Cancel(first);

        Assert.Equal(second, Assert.Single(_sparks.Live(11).ToArray()));
    }

    [Fact]
    public void EventsWithoutContactHaveNoSpark()
    {
        _sparks.Play(Event(10, FrameEventKind.Swing));
        _sparks.Play(Event(10, FrameEventKind.Landing));

        Assert.Equal(0, _sparks.Live(10).Length);
    }

    private static FrameEvent Event(int frame, FrameEventKind kind) =>
        new(frame, kind, fighter: 0, occurrence: 0, new IntVector2(1_000, 50_000), detail: 1);
}
