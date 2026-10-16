using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Netcode.Drivers;
using Sim.Input;
using Sim.Rules;

namespace Netcode.Replays;

/// <summary>Saves a <see cref="Replay"/> as a replay file (<see cref="ReplayFormat"/>).</summary>
public static class ReplayWriter
{
    /// <summary>Writes the replay to <paramref name="stream"/> and leaves the stream open.</summary>
    /// <exception cref="ArgumentException">The replay is longer than the format allows.</exception>
    public static void Write(Stream stream, Replay replay)
    {
        if (replay.Inputs.Count > ReplayFormat.MaxFrames)
        {
            throw new ArgumentException(
                $"A replay holds at most {ReplayFormat.MaxFrames} frames, got {replay.Inputs.Count}.", nameof(replay));
        }

        using (var plain = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            plain.Write(ReplayFormat.Magic);
            plain.Write(ReplayFormat.Version);
        }

        using var compressed = new ZLibStream(stream, CompressionLevel.Optimal, leaveOpen: true);
        using var writer = new BinaryWriter(compressed, Encoding.UTF8, leaveOpen: true);
        WriteHeader(writer, replay.Header);
        writer.Write(replay.Inputs.Count);
        WriteInputRuns(writer, replay.Inputs);
    }

    private static void WriteHeader(BinaryWriter writer, ReplayHeader header)
    {
        WriteField(writer, Encoding.UTF8.GetBytes(header.GameVersion));
        WriteField(writer, header.DataHash);
        writer.Write(header.RulesHash);
        ReadOnlySpan<byte> rules = MemoryMarshal.AsBytes(new ReadOnlySpan<MatchRules>(in header.Rules));
        writer.Write(rules.Length);
        writer.Write(rules);
        WritePlayer(writer, header.Player1);
        WritePlayer(writer, header.Player2);
    }

    private static void WritePlayer(BinaryWriter writer, ReplayPlayer player)
    {
        WriteField(writer, Encoding.UTF8.GetBytes(player.CharacterId));
        writer.Write(player.Colour);
    }

    // ReplayHeader keeps every field within a length byte.
    private static void WriteField(BinaryWriter writer, ReadOnlySpan<byte> bytes)
    {
        writer.Write((byte)bytes.Length);
        writer.Write(bytes);
    }

    // Inputs are mostly held for many frames, so runs shrink them well before compression.
    private static void WriteInputRuns(BinaryWriter writer, IReadOnlyList<FrameInputs> inputs)
    {
        int start = 0;
        for (int frame = 1; frame <= inputs.Count; frame++)
        {
            if (frame < inputs.Count && inputs[frame] == inputs[start])
            {
                continue;
            }

            (InputWord player1, InputWord player2) = inputs[start];
            writer.Write7BitEncodedInt(frame - start);
            writer.Write(player1.Bits);
            writer.Write(player2.Bits);
            start = frame;
        }
    }
}
