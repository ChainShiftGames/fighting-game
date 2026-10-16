using System.Globalization;
using System.IO;
using Netcode.Desync;

namespace DesyncDiff;

/// <summary>Prints a <see cref="DumpDiff"/> for a developer: what differs first, by field name.</summary>
public static class DiffReport
{
    public static void Write(TextWriter output, DesyncDump first, DesyncDump second, DumpDiff diff)
    {
        output.WriteLine($"A: {Describe(first)}");
        output.WriteLine($"B: {Describe(second)}");
        foreach (string warning in diff.Warnings)
        {
            output.WriteLine($"Warning: {warning}");
        }

        output.WriteLine($"First mismatched frame: {first.FirstMismatchFrame} (last agreed: {first.LastAgreedFrame})");
        foreach (StateDifference state in diff.States)
        {
            WriteState(output, state);
        }

        foreach (int frame in diff.FramesInOneDumpOnly)
        {
            output.WriteLine($"Frame {frame}: only one dump holds this state; not compared.");
        }

        WriteInputs(output, diff);
    }

    private static string Describe(DesyncDump dump) =>
        $"P{dump.LocalPlayer}, version {dump.GameVersion}, data {dump.DataHash}, " +
        $"checksum {dump.LocalChecksum} at frame {dump.FirstMismatchFrame}";

    private static void WriteState(TextWriter output, StateDifference state)
    {
        if (state.Fields.Count == 0)
        {
            output.WriteLine($"Frame {state.Frame}: states identical.");
            return;
        }

        output.WriteLine($"Frame {state.Frame}: {state.Fields.Count} field(s) differ:");
        foreach (FieldDifference field in state.Fields)
        {
            output.WriteLine($"  {field.Field}: A={Value(field.First)} B={Value(field.Second)}");
        }
    }

    private static void WriteInputs(TextWriter output, DumpDiff diff)
    {
        if (diff.Inputs.Count == 0)
        {
            output.WriteLine($"Inputs: identical in the {diff.SharedInputFrames} frame(s) both dumps hold.");
            return;
        }

        output.WriteLine($"Inputs: {diff.Inputs.Count} frame(s) differ (an input sync bug, not a logic one):");
        foreach (InputDifference inputs in diff.Inputs)
        {
            output.WriteLine(
                $"  Frame {inputs.First.Frame}: A=P1 {inputs.First.Player1:X4} P2 {inputs.First.Player2:X4} " +
                $"B=P1 {inputs.Second.Player1:X4} P2 {inputs.Second.Player2:X4}");
        }
    }

    private static string Value(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "(missing)";
}
