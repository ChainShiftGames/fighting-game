using System;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>
/// Plays each frame event once and cancels the effects of events a rollback removes (ADR-0005 §3), by keeping the
/// ids played for each recent frame and comparing them with every new simulation of that frame.
/// </summary>
/// <remarks>
/// Call <see cref="Simulated"/> after every logic frame the driver runs, re-simulations included, with that frame's
/// events (also when it has none: that is how missing events are found). Per frame, by <see cref="FrameEventId"/>:
/// a new id plays, an id played before keeps its effect running, a played id that is gone is cancelled.
/// Frames older than the remembered window can't be rolled back anymore, so their effects are final and run out
/// on their own. Storage is allocated once, so reconciling inside the driver's frame allocates nothing.
/// </remarks>
public sealed class FrameEventReconciler
{
    private const int NoFrame = -1;

    private readonly IFrameEventEffect _effect;
    private readonly int[] _frameOfSlot;
    private readonly int[] _playedCount;
    private readonly FrameEvent[] _played;

    /// <param name="effect">Receives the plays and cancels.</param>
    /// <param name="rememberedFrames">
    /// Frames kept, newest included: must be more than the deepest rollback, or a re-simulated frame would be
    /// forgotten and its events played twice.
    /// </param>
    public FrameEventReconciler(IFrameEventEffect effect, int rememberedFrames)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rememberedFrames, 1);
        _effect = effect;
        _frameOfSlot = new int[rememberedFrames];
        _frameOfSlot.AsSpan().Fill(NoFrame);
        _playedCount = new int[rememberedFrames];
        _played = new FrameEvent[rememberedFrames * FrameEvents.Capacity];
    }

    /// <summary>Brings the effects of <paramref name="frame"/> in line with the events it just produced.</summary>
    /// <exception cref="InvalidOperationException">The frame is older than the window: a rollback went too deep.</exception>
    public void Simulated(int frame, ReadOnlySpan<FrameEvent> events)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frame);
        int slot = frame % _frameOfSlot.Length;
        if (_frameOfSlot[slot] > frame)
        {
            throw new InvalidOperationException(
                $"Frame {frame} was re-simulated after it left the {_frameOfSlot.Length}-frame effect window.");
        }

        if (_frameOfSlot[slot] < frame)
        {
            // First run of this frame; the slot's older frame is past rollback, so its effects are final.
            _frameOfSlot[slot] = frame;
            _playedCount[slot] = 0;
        }

        Span<FrameEvent> played = _played.AsSpan(slot * FrameEvents.Capacity, FrameEvents.Capacity);
        int kept = CancelMissing(played[.._playedCount[slot]], events);
        _playedCount[slot] = PlayNew(played, kept, events);
    }

    /// <summary>
    /// Cancels every remembered effect and forgets all frames, for a jump to another point of the match (training
    /// restart or savestate): frames run from there may be numbered lower than those remembered.
    /// </summary>
    /// <remarks>Effects live shorter than the window, so cancelling the remembered events stops every one on screen.</remarks>
    public void Reset()
    {
        for (int slot = 0; slot < _frameOfSlot.Length; slot++)
        {
            Span<FrameEvent> played = _played.AsSpan(slot * FrameEvents.Capacity, _playedCount[slot]);
            foreach (FrameEvent frameEvent in played)
            {
                _effect.Cancel(in frameEvent);
            }

            _frameOfSlot[slot] = NoFrame;
            _playedCount[slot] = 0;
        }
    }

    // Keeps the played events that are still there at the front; returns how many.
    private int CancelMissing(Span<FrameEvent> played, ReadOnlySpan<FrameEvent> events)
    {
        int kept = 0;
        for (int i = 0; i < played.Length; i++)
        {
            if (Contains(events, played[i].Id))
            {
                played[kept++] = played[i];
            }
            else
            {
                _effect.Cancel(in played[i]);
            }
        }

        return kept;
    }

    // Plays the events not among the first `kept` played ones and appends them; returns the new played count.
    private int PlayNew(Span<FrameEvent> played, int kept, ReadOnlySpan<FrameEvent> events)
    {
        int count = kept;
        for (int i = 0; i < events.Length; i++)
        {
            if (!Contains(played[..kept], events[i].Id))
            {
                _effect.Play(in events[i]);
                played[count++] = events[i];
            }
        }

        return count;
    }

    private static bool Contains(ReadOnlySpan<FrameEvent> events, FrameEventId id)
    {
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i].Id == id)
            {
                return true;
            }
        }

        return false;
    }
}
