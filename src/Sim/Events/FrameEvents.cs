using System;
using System.Diagnostics.CodeAnalysis;
using Sim.Math;
using Sim.State;

namespace Sim.Events;

/// <summary>The events of the latest frame that had any, kept in the game state so they roll back with it.</summary>
/// <remarks>
/// Writing: stages call <see cref="Emit"/> with the frame being simulated (<c>state.Frame</c> during a tick). The
/// first event of a new frame replaces the previous frame's events, so nothing has to clear the buffer each tick
/// and a restored snapshot can't leak events into the re-simulated frame.
/// Reading: after ticking frame F (so <c>state.Frame</c> is F + 1), <see cref="OfFrame"/>(F) gives that frame's
/// events in emission order, which the fixed tick order makes deterministic; a frame without events reads empty.
/// Game logic doesn't read events: they are output for presentation, and a full buffer drops some.
/// Overflow: at most <see cref="Capacity"/> events per frame. Further events in that frame are dropped (the
/// earliest ones are kept) and counted in <see cref="DroppedOn"/>; both PCs drop the same ones. Capacity is part
/// of the state layout, not a setting.
/// Rollback (ADR-0005 §3): the driver reads each re-simulated frame's events again and presentation compares them
/// by <see cref="FrameEventId"/> with what it already played for that frame: new ids play, missing ids cancel
/// their effects, matching ids keep playing.
/// </remarks>
public struct FrameEvents
{
    /// <summary>Events kept per frame: a fixed layout constant, sized well above a busy frame.</summary>
    public const int Capacity = 32;

    private int _frame;
    private int _count;
    private int _droppedCount;
    private FrameEventSlots _slots;

    /// <summary>Records an event of <paramref name="frame"/>; false if the frame was full and it was dropped.</summary>
    /// <param name="frame">The frame being simulated; never earlier than the last frame written.</param>
    /// <param name="fighter">The fighter it belongs to, or <see cref="FrameEvent.NoFighter"/>.</param>
    /// <param name="detail">Kind-specific value; see <see cref="FrameEventKind"/>.</param>
    public bool Emit(int frame, FrameEventKind kind, int fighter, IntVector2 position, int detail)
    {
        Validate(frame, kind, fighter);
        if (frame != _frame)
        {
            _frame = frame;
            _count = 0;
            _droppedCount = 0;
        }

        if (_count == Capacity)
        {
            _droppedCount++;
            return false;
        }

        _slots[_count] = new FrameEvent(frame, kind, fighter, CountOf(kind, fighter), position, detail);
        _count++;
        return true;
    }

    /// <summary>
    /// The events of <paramref name="frame"/> in emission order; empty if it had none or isn't the latest frame.
    /// </summary>
    [UnscopedRef]
    public readonly ReadOnlySpan<FrameEvent> OfFrame(int frame)
    {
        if (frame != _frame)
        {
            return ReadOnlySpan<FrameEvent>.Empty;
        }

        ReadOnlySpan<FrameEvent> slots = _slots;
        return slots[.._count];
    }

    /// <summary>Events of <paramref name="frame"/> dropped because the buffer was full; 0 in a healthy match.</summary>
    public readonly int DroppedOn(int frame) => frame == _frame ? _droppedCount : 0;

    private readonly int CountOf(FrameEventKind kind, int fighter)
    {
        int count = 0;
        for (int i = 0; i < _count; i++)
        {
            if (_slots[i].Kind == kind && _slots[i].Fighter == fighter)
            {
                count++;
            }
        }

        return count;
    }

    // Bad arguments are a bug in the emitting stage: fail loudly instead of storing an event presentation can't use.
    private readonly void Validate(int frame, FrameEventKind kind, int fighter)
    {
        if (frame < _frame)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frame), frame, $"Frame {_frame} already has events; earlier frames can't be written anymore.");
        }

        if (kind <= FrameEventKind.None)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Events need a kind.");
        }

        if (fighter < FrameEvent.NoFighter || fighter >= MatchState.PlayerCount)
        {
            throw new ArgumentOutOfRangeException(nameof(fighter), fighter, "Not a fighter index or NoFighter.");
        }
    }
}
