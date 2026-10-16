using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Sim.State;

namespace Soak;

/// <summary>
/// Writes what the soak knows about a desync to a text file: how to reproduce it, both peers' checksums around the
/// first mismatched frame, and both states at that frame as raw bytes with the differing offsets listed.
/// </summary>
/// <remarks>A stopgap until the desync dumps and diff tool of P2-09; it names byte offsets, not fields.</remarks>
internal static class DesyncDump
{
    private const int ChecksumFramesBefore = 10;
    private const int BytesPerLine = 32;
    private const int MaxListedOffsets = 200;

    public static string Write(
        string directory, string reproduction, int frame, RecordingSimulation player1, RecordingSimulation player2)
    {
        var text = new StringBuilder();
        text.AppendLine(reproduction);
        text.AppendLine($"First mismatched confirmed frame: {frame}");
        text.AppendLine();
        text.AppendLine("Frame  P1 checksum       P2 checksum");
        for (int at = Math.Max(1, frame - ChecksumFramesBefore); at <= frame; at++)
        {
            text.AppendLine($"{at,5}  {player1.ChecksumAt(at):x16}  {player2.ChecksumAt(at):x16}");
        }

        text.AppendLine();
        if (player1.TryGetState(frame, out MatchState first) && player2.TryGetState(frame, out MatchState second))
        {
            AppendStates(text, in first, in second);
        }
        else
        {
            text.AppendLine($"States at frame {frame} are no longer kept.");
        }

        Directory.CreateDirectory(directory);
        string path = Path.GetFullPath(Path.Combine(directory, $"desync-frame-{frame}.txt"));
        File.WriteAllText(path, text.ToString());
        return path;
    }

    private static void AppendStates(StringBuilder text, in MatchState first, in MatchState second)
    {
        ReadOnlySpan<byte> player1 = MemoryMarshal.AsBytes(new ReadOnlySpan<MatchState>(in first));
        ReadOnlySpan<byte> player2 = MemoryMarshal.AsBytes(new ReadOnlySpan<MatchState>(in second));
        text.Append("Differing byte offsets in MatchState (P1/P2):");
        int listed = 0;
        for (int offset = 0; offset < player1.Length && listed < MaxListedOffsets; offset++)
        {
            if (player1[offset] != player2[offset])
            {
                text.Append($" {offset}:{player1[offset]:x2}/{player2[offset]:x2}");
                listed++;
            }
        }

        text.AppendLine();
        text.AppendLine();
        AppendBytes(text, "P1 state", player1);
        AppendBytes(text, "P2 state", player2);
    }

    private static void AppendBytes(StringBuilder text, string title, ReadOnlySpan<byte> bytes)
    {
        text.AppendLine($"{title} ({bytes.Length} bytes):");
        for (int offset = 0; offset < bytes.Length; offset += BytesPerLine)
        {
            int length = Math.Min(BytesPerLine, bytes.Length - offset);
            text.AppendLine($"{offset,6}: {Convert.ToHexString(bytes.Slice(offset, length))}");
        }

        text.AppendLine();
    }
}
