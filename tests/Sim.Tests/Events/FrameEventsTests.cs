using System;
using Sim.Events;
using Sim.Math;
using Sim.State;
using Xunit;

namespace Sim.Tests.Events;

public sealed class FrameEventsTests
{
    [Fact]
    public void EventsAreReadBackForTheirFrameInEmissionOrder()
    {
        FrameEvents events = default;
        events.Emit(5, FrameEventKind.Hit, 0, new IntVector2(100, 200), 7);
        events.Emit(5, FrameEventKind.Landing, 1, new IntVector2(300, 0), 0);

        ReadOnlySpan<FrameEvent> frame = events.OfFrame(5);

        Assert.Equal(2, frame.Length);
        Assert.Equal(new FrameEventId(5, FrameEventKind.Hit, 0, 0), frame[0].Id);
        Assert.Equal(new IntVector2(100, 200), frame[0].Position);
        Assert.Equal(7, frame[0].Detail);
        Assert.Equal(new FrameEventId(5, FrameEventKind.Landing, 1, 0), frame[1].Id);
    }

    [Fact]
    public void OccurrenceCountsEventsOfTheSameKindAndFighterWithinAFrame()
    {
        FrameEvents events = default;
        events.Emit(3, FrameEventKind.Hit, 0, IntVector2.Zero, 0);
        events.Emit(3, FrameEventKind.Hit, 1, IntVector2.Zero, 0);
        events.Emit(3, FrameEventKind.Block, 0, IntVector2.Zero, 0);
        events.Emit(3, FrameEventKind.Hit, 0, IntVector2.Zero, 0);

        ReadOnlySpan<FrameEvent> frame = events.OfFrame(3);

        int[] occurrences = [frame[0].Occurrence, frame[1].Occurrence, frame[2].Occurrence, frame[3].Occurrence];
        Assert.Equal([0, 0, 0, 1], occurrences);
    }

    [Fact]
    public void TheFirstEventOfALaterFrameReplacesTheEarlierFramesEvents()
    {
        FrameEvents events = default;
        events.Emit(1, FrameEventKind.Hit, 0, IntVector2.Zero, 0);
        events.Emit(1, FrameEventKind.Hit, 0, IntVector2.Zero, 0);

        events.Emit(4, FrameEventKind.Hit, 0, IntVector2.Zero, 0);

        Assert.True(events.OfFrame(1).IsEmpty);
        ReadOnlySpan<FrameEvent> frame = events.OfFrame(4);
        Assert.Equal(1, frame.Length);
        Assert.Equal(0, frame[0].Occurrence);
    }

    [Fact]
    public void AFrameWithoutEventsReadsEmpty()
    {
        FrameEvents events = default;
        events.Emit(2, FrameEventKind.Swing, 1, IntVector2.Zero, 3);

        Assert.True(events.OfFrame(3).IsEmpty);
        Assert.True(events.OfFrame(0).IsEmpty);
        FrameEvents fresh = default;
        Assert.True(fresh.OfFrame(0).IsEmpty);
    }

    [Fact]
    public void AFullFrameKeepsTheEarliestEventsAndCountsTheDroppedOnes()
    {
        FrameEvents events = default;
        for (int i = 0; i < FrameEvents.Capacity; i++)
        {
            Assert.True(events.Emit(9, FrameEventKind.Hit, 0, IntVector2.Zero, i));
        }

        Assert.False(events.Emit(9, FrameEventKind.Hit, 0, IntVector2.Zero, -1));
        Assert.False(events.Emit(9, FrameEventKind.Block, 1, IntVector2.Zero, -1));

        ReadOnlySpan<FrameEvent> frame = events.OfFrame(9);
        Assert.Equal(FrameEvents.Capacity, frame.Length);
        Assert.Equal(FrameEvents.Capacity - 1, frame[^1].Detail);
        Assert.Equal(2, events.DroppedOn(9));

        Assert.True(events.Emit(10, FrameEventKind.Hit, 0, IntVector2.Zero, 0));
        Assert.Equal(0, events.DroppedOn(10));
        Assert.Equal(0, events.DroppedOn(9));
    }

    [Fact]
    public void AMatchWideEventUsesNoFighter()
    {
        FrameEvents events = default;

        Assert.True(events.Emit(0, FrameEventKind.Landing, FrameEvent.NoFighter, IntVector2.Zero, 0));
        Assert.Equal(FrameEvent.NoFighter, events.OfFrame(0)[0].Fighter);
    }

    [Theory]
    [InlineData(4, FrameEventKind.Hit, 0)]
    [InlineData(5, FrameEventKind.None, 0)]
    [InlineData(5, FrameEventKind.Hit, MatchState.PlayerCount)]
    [InlineData(5, FrameEventKind.Hit, -2)]
    public void InvalidEventsThrowAndChangeNothing(int frame, FrameEventKind kind, int fighter)
    {
        var events = new FrameEvents[1];
        events[0].Emit(5, FrameEventKind.Block, 1, IntVector2.Zero, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => events[0].Emit(frame, kind, fighter, IntVector2.Zero, 0));
        Assert.Equal(1, events[0].OfFrame(5).Length);
    }
}
