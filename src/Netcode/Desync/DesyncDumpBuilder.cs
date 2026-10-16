using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Sim.State;

namespace Netcode.Desync;

/// <summary>Builds this PC's desync dump from its frame history and the mismatch it found.</summary>
public static class DesyncDumpBuilder
{
    public static DesyncDump Build<TState>(
        DesyncDumpHeader header, ChecksumMismatch mismatch, FrameHistory<TState> history, DateTimeOffset writtenAt)
        where TState : unmanaged
    {
        return new DesyncDump
        {
            FormatVersion = DesyncDump.CurrentFormatVersion,
            GameVersion = header.GameVersion,
            DataHash = header.DataHash,
            LocalPlayer = header.LocalIsPlayer1 ? 1 : 2,
            WrittenAt = writtenAt,
            LastAgreedFrame = mismatch.LastAgreedFrame,
            FirstMismatchFrame = mismatch.Frame,
            LocalChecksum = Hex(mismatch.LocalChecksum),
            RemoteChecksum = Hex(mismatch.RemoteChecksum),
            LastAgreedState = TryDumpState(history, mismatch.LastAgreedFrame),
            FirstMismatchState = TryDumpState(history, mismatch.Frame),
            Inputs = DumpInputs(history, mismatch.Frame),
        };
    }

    /// <summary>A checksum as the dumps and the diff tool show it.</summary>
    public static string Hex(ulong checksum) => checksum.ToString("X16", CultureInfo.InvariantCulture);

    private static DumpedState? TryDumpState<TState>(FrameHistory<TState> history, int frame)
        where TState : unmanaged
    {
        if (!history.Contains(frame))
        {
            return null;
        }

        ref readonly TState state = ref history.Get(frame).State;
        return new DumpedState
        {
            Frame = frame,
            Checksum = Hex(StateChecksum.Compute(in state)),
            Fields = StateFields.Read(in state),
            Bytes = Convert.ToBase64String(MemoryMarshal.AsBytes(new ReadOnlySpan<TState>(in state))),
        };
    }

    private static List<DumpedInputs> DumpInputs<TState>(FrameHistory<TState> history, int lastFrame)
        where TState : unmanaged
    {
        var inputs = new List<DumpedInputs>();
        for (int frame = Math.Max(1, lastFrame - history.Capacity + 1); frame <= lastFrame; frame++)
        {
            if (history.Contains(frame))
            {
                ref readonly FrameRecord<TState> record = ref history.Get(frame);
                inputs.Add(new DumpedInputs(frame, record.Player1.Bits, record.Player2.Bits));
            }
        }

        return inputs;
    }
}
