using System;
using System.IO;
using FightingGame.Online;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Input;
using Sim.Rules;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class ReplayFolderTests : IDisposable
{
    private static readonly DateTime _savedAt = new(2026, 10, 17, 14, 3, 22, DateTimeKind.Local);

    private readonly string _path = Path.Combine(Path.GetTempPath(), "fg-replays-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void NamesAReplayAfterWhenItWasSavedAndItsKind()
    {
        Assert.Equal("2026-10-17_14-03-22_local.fgreplay", ReplayFolder.FileName(_savedAt, "local"));
        Assert.Equal("2026-10-17_14-03-22_online-p2_3.fgreplay", ReplayFolder.FileName(_savedAt, "online-p2", 3));
    }

    [Fact]
    public void SavesIntoAFolderItCreatesAndLoadsTheReplayBack()
    {
        var folder = new ReplayFolder(_path);
        Replay replay = ReplayOf(GameVersion.Current);

        string file = folder.Save(replay, _savedAt, "local");

        Assert.Equal(Path.Combine(_path, "2026-10-17_14-03-22_local.fgreplay"), file);
        Replay loaded = ReplayFolder.Load(file, TestGameData.Data);
        Assert.Equal(replay.Inputs, loaded.Inputs);
    }

    [Fact]
    public void TwoReplaysSavedInOneSecondGetTheirOwnFiles()
    {
        var folder = new ReplayFolder(_path);

        string first = folder.Save(ReplayOf(GameVersion.Current), _savedAt, "local");
        string second = folder.Save(ReplayOf(GameVersion.Current), _savedAt, "local");

        Assert.NotEqual(first, second);
        Assert.EndsWith("_local_2.fgreplay", second, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAReplayOfAnotherBuild()
    {
        string file = new ReplayFolder(_path).Save(ReplayOf("0.9.0+other"), _savedAt, "local");

        var rejected = Assert.Throws<ReplayRejectedException>(() => ReplayFolder.Load(file, TestGameData.Data));

        Assert.Equal(ReplayRejection.OtherGameVersion, rejected.Reason);
    }

    [Fact]
    public void ListsReplaysNewestFirstAndNoneBeforeTheFirstSave()
    {
        var folder = new ReplayFolder(_path);
        Assert.Empty(folder.Files());

        string older = folder.Save(ReplayOf(GameVersion.Current), _savedAt, "local");
        string newer = folder.Save(ReplayOf(GameVersion.Current), _savedAt.AddMinutes(1), "online-p1");
        File.WriteAllText(Path.Combine(_path, "notes.txt"), "not a replay");

        Assert.Equal([newer, older], folder.Files());
    }

    [Fact]
    public void ReadsWhenAReplayWasSavedFromItsName()
    {
        Assert.Equal(_savedAt, ReplayFolder.SavedAt(Path.Combine(_path, ReplayFolder.FileName(_savedAt, "local", 2))));
        Assert.Null(ReplayFolder.SavedAt("my replay.fgreplay"));
    }

    [Fact]
    public void DeletesOnlyItsOwnReplays()
    {
        var folder = new ReplayFolder(_path);
        string file = folder.Save(ReplayOf(GameVersion.Current), _savedAt, "local");

        Assert.Throws<ArgumentException>(() => folder.Delete(Path.Combine(Path.GetTempPath(), "elsewhere.fgreplay")));
        folder.Delete(file);

        Assert.False(File.Exists(file));
    }

    public void Dispose()
    {
        if (Directory.Exists(_path))
        {
            Directory.Delete(_path, recursive: true);
        }
    }

    private static Replay ReplayOf(string gameVersion)
    {
        MatchRules rules = TestGameData.Data.CreateMatchRules();
        var header = new ReplayHeader(
            gameVersion,
            TestGameData.Data.Hash.ToBytes(),
            in rules,
            new ReplayPlayer("testman", 0),
            new ReplayPlayer("testman", 0));
        return new Replay(header, [new FrameInputs(new InputWord(InputWord.Right), default), default]);
    }
}
