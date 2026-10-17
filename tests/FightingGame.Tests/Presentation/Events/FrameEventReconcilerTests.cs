using System;
using FightingGame.Presentation.Events;
using Sim.Events;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class FrameEventReconcilerTests
{
    private const int RememberedFrames = 4;

    private readonly RecordingEffect _effect = new();
    private readonly FrameEventReconciler _reconciler;

    public FrameEventReconcilerTests()
    {
        _reconciler = new FrameEventReconciler(_effect, RememberedFrames);
    }

    [Fact]
    public void PlaysANewEventOnce()
    {
        FrameEvent hit = Hit(frame: 5);

        _reconciler.Simulated(5, [hit]);

        Assert.Equal([hit], _effect.Played);
        Assert.Empty(_effect.Cancelled);
    }

    [Fact]
    public void DoesNotReplayAReSimulatedEvent()
    {
        _reconciler.Simulated(5, [Hit(frame: 5)]);
        _reconciler.Simulated(6, []);

        // Rollback: frames 5 and 6 run again; the hit lands a few subpixels away, but it is the same hit.
        _reconciler.Simulated(5, [Hit(frame: 5, x: 1_250)]);
        _reconciler.Simulated(6, []);

        Assert.Single(_effect.Played);
        Assert.Empty(_effect.Cancelled);
    }

    [Fact]
    public void CancelsAnEventMissingAfterRollback()
    {
        FrameEvent hit = Hit(frame: 5);
        _reconciler.Simulated(5, [hit]);
        _reconciler.Simulated(6, []);

        // Rollback: with the opponent's real input the attack whiffs.
        _reconciler.Simulated(5, []);

        Assert.Equal([hit], _effect.Cancelled);
    }

    [Fact]
    public void CancelsOnlyTheEventsThatDisappeared()
    {
        FrameEvent swing = Event(5, FrameEventKind.Swing);
        FrameEvent hit = Hit(frame: 5);
        _reconciler.Simulated(5, [swing, hit]);

        _reconciler.Simulated(5, [swing]);

        Assert.Equal([hit], _effect.Cancelled);
        Assert.Equal([swing, hit], _effect.Played);
    }

    [Fact]
    public void CancelsAndReplaysAnEventThatMovedToAnotherFrame()
    {
        FrameEvent early = Hit(frame: 5);
        FrameEvent late = Hit(frame: 6);
        _reconciler.Simulated(5, [early]);
        _reconciler.Simulated(6, []);

        _reconciler.Simulated(5, []);
        _reconciler.Simulated(6, [late]);

        Assert.Equal([early], _effect.Cancelled);
        Assert.Equal([early, late], _effect.Played);
    }

    [Fact]
    public void ACancelledEventThatComesBackPlaysAgain()
    {
        FrameEvent hit = Hit(frame: 5);
        _reconciler.Simulated(5, [hit]);
        _reconciler.Simulated(5, []);

        _reconciler.Simulated(5, [hit]);

        Assert.Equal([hit, hit], _effect.Played);
        Assert.Equal([hit], _effect.Cancelled);
    }

    [Fact]
    public void AConfirmedEventSurvives()
    {
        FrameEvent hit = Hit(frame: 5);
        _reconciler.Simulated(5, [hit]);

        // Frame 5 leaves the window: it can't be rolled back anymore, and reusing its slot cancels nothing.
        for (int frame = 6; frame <= 5 + RememberedFrames; frame++)
        {
            _reconciler.Simulated(frame, []);
        }

        Assert.Equal([hit], _effect.Played);
        Assert.Empty(_effect.Cancelled);
    }

    [Fact]
    public void RefusesARollbackDeeperThanItsWindow()
    {
        for (int frame = 0; frame <= RememberedFrames; frame++)
        {
            _reconciler.Simulated(frame, []);
        }

        Assert.Throws<InvalidOperationException>(() => _reconciler.Simulated(0, []));
    }

    [Fact]
    public void ResetCancelsWhatPlayedAndAcceptsLowerFramesAfterwards()
    {
        FrameEvent hit = Hit(frame: 10);
        for (int frame = 0; frame <= 10; frame++)
        {
            _reconciler.Simulated(frame, frame == 10 ? [hit] : []);
        }

        _reconciler.Reset();
        FrameEvent fresh = Hit(frame: 0);
        _reconciler.Simulated(0, [fresh]);

        Assert.Equal([hit], _effect.Cancelled);
        Assert.Equal([hit, fresh], _effect.Played);
    }

    [Fact]
    public void TellsEventsOfTheSameKindApartByOccurrence()
    {
        FrameEvent first = Event(5, FrameEventKind.ProjectileHit, occurrence: 0);
        FrameEvent second = Event(5, FrameEventKind.ProjectileHit, occurrence: 1);
        _reconciler.Simulated(5, [first, second]);

        _reconciler.Simulated(5, [first]);

        Assert.Equal([second], _effect.Cancelled);
    }

    private static FrameEvent Hit(int frame, int x = 1_000) =>
        new(frame, FrameEventKind.Hit, fighter: 0, occurrence: 0, new IntVector2(x, 50_000), detail: 1);

    private static FrameEvent Event(int frame, FrameEventKind kind, int occurrence = 0) =>
        new(frame, kind, fighter: 0, occurrence, IntVector2.Zero, detail: 0);
}
