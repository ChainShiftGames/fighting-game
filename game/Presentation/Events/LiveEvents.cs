using System;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>
/// The played events whose effect is still on screen, oldest first: added on play, removed on cancel or when they
/// are older than the effect's lifetime in logic frames (ARCHITECTURE.md §9).
/// </summary>
/// <remarks>Fixed capacity, allocated once; when full, the oldest event makes room for the new one.</remarks>
public sealed class LiveEvents
{
    private readonly FrameEvent[] _events;
    private int _count;

    public LiveEvents(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _events = new FrameEvent[capacity];
    }

    public ReadOnlySpan<FrameEvent> All => _events.AsSpan(0, _count);

    public void Add(in FrameEvent frameEvent)
    {
        if (_count == _events.Length)
        {
            RemoveAt(0);
        }

        _events[_count++] = frameEvent;
    }

    public void Remove(FrameEventId id)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_events[i].Id == id)
            {
                RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>Drops events whose age at <paramref name="latestFrame"/> is <paramref name="lifetimeFrames"/> or more.</summary>
    /// <param name="latestFrame">The newest simulated frame, the one on screen (<c>MatchState.Frame</c> − 1).</param>
    public void Expire(int latestFrame, int lifetimeFrames)
    {
        int kept = 0;
        for (int i = 0; i < _count; i++)
        {
            if (latestFrame - _events[i].Frame < lifetimeFrames)
            {
                _events[kept++] = _events[i];
            }
        }

        _count = kept;
    }

    private void RemoveAt(int index)
    {
        _events.AsSpan(index + 1, _count - index - 1).CopyTo(_events.AsSpan(index));
        _count--;
    }
}
