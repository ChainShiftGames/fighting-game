using System;
using Sim.Input;

namespace Netcode.Drivers.Training;

/// <summary>
/// The dummy's recordings: <see cref="Count"/> slots of input words relative to its facing, kept in memory while
/// training is open (not saved). Recording into a slot replaces what it held.
/// </summary>
public sealed class RecordingSlots
{
    /// <summary>Slots in the training menu.</summary>
    public const int Count = 5;

    /// <summary>The longest recording, 10 seconds at 60 frames per second (developer setting).</summary>
    public const int MaxFrames = 600;

    private readonly InputWord[][] _slots = new InputWord[Count][];
    private readonly int[] _lengths = new int[Count];
    private int _recordingSlot = -1;

    public RecordingSlots()
    {
        for (int slot = 0; slot < Count; slot++)
        {
            _slots[slot] = new InputWord[MaxFrames];
        }
    }

    /// <summary>The slot being recorded into, or null when not recording.</summary>
    public int? RecordingSlot => _recordingSlot >= 0 ? _recordingSlot : null;

    /// <summary>Recorded frames in a slot; 0 when empty.</summary>
    public int Length(int slot) => _lengths[slot];

    public InputWord At(int slot, int frame) => _slots[slot][frame];

    /// <summary>Empties <paramref name="slot"/> and records into it from the next <see cref="Add"/>.</summary>
    public void Start(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, Count);
        _lengths[slot] = 0;
        _recordingSlot = slot;
    }

    public void Stop() => _recordingSlot = -1;

    /// <summary>Adds a frame to the slot being recorded, if any; recording stops by itself when the slot is full.</summary>
    public void Add(InputWord relativeInput)
    {
        if (_recordingSlot < 0)
        {
            return;
        }

        _slots[_recordingSlot][_lengths[_recordingSlot]++] = relativeInput;
        if (_lengths[_recordingSlot] == MaxFrames)
        {
            Stop();
        }
    }
}
