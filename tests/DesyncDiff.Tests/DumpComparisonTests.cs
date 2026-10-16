using System;
using System.Collections.Generic;
using System.IO;
using Netcode.Desync;
using Xunit;

namespace DesyncDiff.Tests;

public sealed class DumpComparisonTests
{
    [Fact]
    public void DifferingFieldsAreListedByNameInLayoutOrder()
    {
        DesyncDump first = Dump(1, new() { ["Frame"] = 7, ["Fighters[0].Health"] = 1000, ["Fighters[1].Health"] = 900 });
        DesyncDump second = Dump(2, new() { ["Frame"] = 7, ["Fighters[0].Health"] = 999, ["Fighters[1].Health"] = 901 });

        DumpDiff diff = DumpComparison.Compare(first, second);

        StateDifference state = Assert.Single(diff.States);
        Assert.Equal(7, state.Frame);
        Assert.Equal(
            [new FieldDifference("Fighters[0].Health", 1000, 999), new FieldDifference("Fighters[1].Health", 900, 901)],
            state.Fields);
        Assert.Empty(diff.Warnings);
    }

    [Fact]
    public void FieldsOnlyOneDumpHasAreListedAsMissing()
    {
        DesyncDump first = Dump(1, new() { ["Frame"] = 7, ["Old"] = 1 });
        DesyncDump second = Dump(2, new() { ["Frame"] = 7, ["New"] = 2 });

        StateDifference state = Assert.Single(DumpComparison.Compare(first, second).States);

        Assert.Equal([new FieldDifference("Old", 1, null), new FieldDifference("New", null, 2)], state.Fields);
    }

    [Fact]
    public void DifferentBuildsAndDifferingInputsAreReported()
    {
        DesyncDump first = Dump(1, new() { ["Frame"] = 7 });
        DesyncDump second = Dump(2, new() { ["Frame"] = 7 }, gameVersion: "2.0", player2Input: 16);

        DumpDiff diff = DumpComparison.Compare(first, second);

        Assert.Contains(diff.Warnings, warning => warning.Contains("Game versions differ", StringComparison.Ordinal));
        InputDifference inputs = Assert.Single(diff.Inputs);
        Assert.Equal(7, inputs.First.Frame);
        Assert.Equal(16, inputs.Second.Player2);
    }

    [Fact]
    public void TheCommandRefusesAnythingButTwoDumps()
    {
        var error = new StringWriter();

        Assert.Equal(DesyncDiffCommand.Failed, DesyncDiffCommand.Run(["only-one.json"], TextWriter.Null, error));
        Assert.Contains("Usage", error.ToString(), StringComparison.Ordinal);
        Assert.Equal(
            DesyncDiffCommand.Failed,
            DesyncDiffCommand.Run(["missing-a.json", "missing-b.json"], TextWriter.Null, TextWriter.Null));
    }

    private static DesyncDump Dump(
        int localPlayer, Dictionary<string, long> fields, string gameVersion = "1.0", int player2Input = 0) => new()
        {
            FormatVersion = DesyncDump.CurrentFormatVersion,
            GameVersion = gameVersion,
            DataHash = "ABCD",
            LocalPlayer = localPlayer,
            WrittenAt = DateTimeOffset.UnixEpoch,
            LastAgreedFrame = 6,
            FirstMismatchFrame = 7,
            LocalChecksum = "01",
            RemoteChecksum = "02",
            FirstMismatchState = new DumpedState { Frame = 7, Checksum = "01", Fields = fields, Bytes = string.Empty },
            Inputs = [new DumpedInputs(7, 0, player2Input)],
        };
}
