using System;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>
/// Which event each of a fixed set of sound players is playing, so a cancelled event stops its own sound only.
/// </summary>
/// <remarks>Voices are handed out in turn, so a new sound takes over the voice whose sound started longest ago.</remarks>
public sealed class SoundVoices
{
    /// <summary>What <see cref="Release"/> returns when no voice plays the event (anymore).</summary>
    public const int NoVoice = -1;

    private readonly FrameEventId?[] _eventOf;
    private int _next;

    public SoundVoices(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        _eventOf = new FrameEventId?[count];
    }

    /// <summary>The voice that plays <paramref name="id"/>'s sound from now on.</summary>
    public int Assign(FrameEventId id)
    {
        int voice = _next;
        _eventOf[voice] = id;
        _next = (_next + 1) % _eventOf.Length;
        return voice;
    }

    /// <summary>The voice playing <paramref name="id"/>'s sound, now free; <see cref="NoVoice"/> if none.</summary>
    public int Release(FrameEventId id)
    {
        int voice = Array.IndexOf(_eventOf, id);
        if (voice != NoVoice)
        {
            _eventOf[voice] = null;
        }

        return voice;
    }
}
