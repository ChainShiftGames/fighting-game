using System;
using System.Collections.Generic;
using System.IO;
using Netcode.Desync;
using Xunit;

namespace Netcode.Tests.Desync;

public sealed class DesyncDumpFileTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    [Fact]
    public void ADumpReadsBackAsWritten()
    {
        DesyncDump written = Dump(formatVersion: DesyncDump.CurrentFormatVersion);

        DesyncDump read = DesyncDumpFile.Read(DesyncDumpFile.Write(_folder.Path, written));

        Assert.Equal(written.GameVersion, read.GameVersion);
        Assert.Equal(written.FirstMismatchFrame, read.FirstMismatchFrame);
        Assert.Equal(written.Inputs, read.Inputs);
        Assert.Equal(written.FirstMismatchState!.Fields, read.FirstMismatchState!.Fields);
        Assert.Null(read.LastAgreedState);
    }

    [Fact]
    public void ASecondDumpOfTheSameMomentGetsItsOwnFile()
    {
        DesyncDump dump = Dump(formatVersion: DesyncDump.CurrentFormatVersion);

        string first = DesyncDumpFile.Write(_folder.Path, dump);
        string second = DesyncDumpFile.Write(_folder.Path, dump);

        Assert.NotEqual(first, second);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    [Fact]
    public void ADumpOfAnotherFormatIsRejected()
    {
        string path = DesyncDumpFile.Write(_folder.Path, Dump(formatVersion: DesyncDump.CurrentFormatVersion + 1));

        Assert.Throws<InvalidDataException>(() => DesyncDumpFile.Read(path));
    }

    public void Dispose() => _folder.Dispose();

    private static DesyncDump Dump(int formatVersion) => new()
    {
        FormatVersion = formatVersion,
        GameVersion = "1.2.3",
        DataHash = "ABCD",
        LocalPlayer = 2,
        WrittenAt = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero),
        LastAgreedFrame = 0,
        FirstMismatchFrame = 7,
        LocalChecksum = "01",
        RemoteChecksum = "02",
        FirstMismatchState = new DumpedState
        {
            Frame = 7,
            Checksum = "01",
            Fields = new Dictionary<string, long> { ["Fighters[1].Health"] = 1000, ["Frame"] = 7 },
            Bytes = "AAAA",
        },
        Inputs = [new DumpedInputs(7, 1, 2)],
    };
}
