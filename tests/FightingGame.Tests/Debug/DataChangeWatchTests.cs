using System;
using System.IO;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

/// <summary>Data saved from outside the game restarts a local or training match once the save is done (P3-43).</summary>
public sealed class DataChangeWatchTests : IDisposable
{
    private const long Interval = 10;

    private readonly string _folder = Directory.CreateTempSubdirectory("data-watch-").FullName;
    private int _fingerprint;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void AChangeIsReportedOnceItHasSettled()
    {
        var watch = new DataChangeWatch(() => _fingerprint, Interval, now: 0);

        Assert.False(watch.HasSettledChange(10));
        _fingerprint = 1;
        Assert.False(watch.HasSettledChange(15));
        Assert.False(watch.HasSettledChange(20));
        // Still being written: one more file lands before the next check.
        _fingerprint = 2;
        Assert.False(watch.HasSettledChange(30));
        Assert.True(watch.HasSettledChange(40));
        Assert.False(watch.HasSettledChange(50));
    }

    [Fact]
    public void DataTheGameAcceptedIsNotReportedAgain()
    {
        var watch = new DataChangeWatch(() => _fingerprint, Interval, now: 0);
        _fingerprint = 1;

        watch.Accept(5);

        Assert.False(watch.HasSettledChange(15));
        Assert.False(watch.HasSettledChange(25));
    }

    [Fact]
    public void TheFingerprintFollowsSavesAddsAndDeletes()
    {
        string file = Path.Combine(_folder, "5a.json");
        File.WriteAllText(file, "{}");
        int first = DataFolderFingerprint.Of(_folder);
        Assert.Equal(first, DataFolderFingerprint.Of(_folder));

        File.WriteAllText(file, "{ }");
        int saved = DataFolderFingerprint.Of(_folder);
        Assert.NotEqual(first, saved);

        Directory.CreateDirectory(Path.Combine(_folder, "moves"));
        File.WriteAllText(Path.Combine(_folder, "moves", "5b.json"), "{}");
        int added = DataFolderFingerprint.Of(_folder);
        Assert.NotEqual(saved, added);

        File.Delete(file);
        Assert.NotEqual(added, DataFolderFingerprint.Of(_folder));
    }
}
