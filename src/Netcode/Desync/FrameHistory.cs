using System;
using Sim.Input;

namespace Netcode.Desync;

/// <summary>The most recent frames this PC ran, so a desync dump can show the states and inputs around it.</summary>
/// <remarks>
/// A fixed ring allocated once; recording copies the state into it. A rollback records the frames it resimulates
/// again, so each slot ends up holding the frame as it ran with the confirmed inputs.
/// </remarks>
public sealed class FrameHistory<TState>
    where TState : unmanaged
{
    private readonly FrameRecord<TState>[] _records;

    public FrameHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _records = new FrameRecord<TState>[capacity];
    }

    public int Capacity => _records.Length;

    public void Record(int frame, ref readonly TState state, InputWord player1, InputWord player2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frame);
        ref FrameRecord<TState> record = ref _records[frame % _records.Length];
        record.Frame = frame;
        record.Player1 = player1;
        record.Player2 = player2;
        record.State = state;
    }

    /// <summary>True when <paramref name="frame"/> was recorded and not yet overwritten by a newer one.</summary>
    public bool Contains(int frame) => frame > 0 && _records[frame % _records.Length].Frame == frame;

    /// <exception cref="InvalidOperationException">The frame was never recorded or is too old to be kept.</exception>
    public ref readonly FrameRecord<TState> Get(int frame)
    {
        if (!Contains(frame))
        {
            throw new InvalidOperationException(
                $"Frame {frame} is not in the desync history (it keeps the last {_records.Length} frames recorded).");
        }

        return ref _records[frame % _records.Length];
    }
}
