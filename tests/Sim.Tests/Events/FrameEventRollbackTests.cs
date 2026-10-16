using System;
using Sim.Events;
using Sim.Input;
using Sim.Pipeline;
using Sim.State;
using Sim.Tests.Pipeline;
using Xunit;

namespace Sim.Tests.Events;

/// <summary>Events are game state: a restored snapshot re-simulates its frames' events (ADR-0005 §3).</summary>
public sealed class FrameEventRollbackTests
{
    private static InputWord Pressed => new(InputWord.Right);

    private readonly TickPipeline _pipeline =
        Pipelines.Create(new TickStages { HitResolution = new EventEmittingStage() });

    [Fact]
    public void AfterATickTheEventsAreReadWithTheFrameJustSimulated()
    {
        MatchState state = default;
        _pipeline.Tick(ref state, Pressed, InputWord.Neutral);

        FrameEvent[] events = state.Events.OfFrame(state.Frame - 1).ToArray();

        Assert.Equal(2, events.Length);
        Assert.All(events, frameEvent => Assert.Equal(0, frameEvent.Frame));
    }

    [Fact]
    public void ResimulatingAFrameReplacesThePredictedEventsAndKeepsTheIdsOfMatchingOnes()
    {
        MatchState state = default;
        _pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        MatchState snapshot = state;

        _pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        FrameEventId[] predicted = Ids(state.Events.OfFrame(1));

        state = snapshot;
        _pipeline.Tick(ref state, Pressed, InputWord.Neutral);
        FrameEventId[] confirmed = Ids(state.Events.OfFrame(1));

        Assert.Equal([new FrameEventId(1, FrameEventKind.Landing, 1, 0)], predicted);
        Assert.Equal(
            [new FrameEventId(1, FrameEventKind.Landing, 1, 0), new FrameEventId(1, FrameEventKind.Hit, 0, 0)],
            confirmed);
    }

    [Fact]
    public void ResimulatingWithTheSameInputsGivesTheSameChecksum()
    {
        MatchState state = default;
        _pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        MatchState snapshot = state;

        _pipeline.Tick(ref state, Pressed, InputWord.Neutral);
        _pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        ulong original = StateChecksum.Compute(in state);

        // A misprediction first, then the rollback that re-simulates the real inputs.
        state = snapshot;
        _pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        state = snapshot;
        _pipeline.Tick(ref state, Pressed, InputWord.Neutral);
        _pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);

        Assert.Equal(original, StateChecksum.Compute(in state));
    }

    [Fact]
    public void EventsChangeTheChecksum()
    {
        MatchState quiet = default;
        MatchState busy = default;
        busy.Events.Emit(0, FrameEventKind.Hit, 0, default, 0);

        Assert.NotEqual(StateChecksum.Compute(in quiet), StateChecksum.Compute(in busy));
    }

    [Fact]
    public void EmittingAndReadingEventsAllocatesNothing()
    {
        MatchState state = default;
        _pipeline.Tick(ref state, Pressed, InputWord.Neutral);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int read = 0;
        for (int frame = 0; frame < 1000; frame++)
        {
            _pipeline.Tick(ref state, new InputWord((ushort)(frame & 15)), InputWord.Neutral);
            read += state.Events.OfFrame(state.Frame - 1).Length;
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.True(read > 1000);
    }

    private static FrameEventId[] Ids(ReadOnlySpan<FrameEvent> events)
    {
        var ids = new FrameEventId[events.Length];
        for (int i = 0; i < events.Length; i++)
        {
            ids[i] = events[i].Id;
        }

        return ids;
    }
}
