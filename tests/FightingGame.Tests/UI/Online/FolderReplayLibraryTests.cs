using System;
using System.Collections.Generic;
using System.IO;
using FightingGame.Online;
using FightingGame.UI.Online;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Rules;
using Xunit;

namespace FightingGame.Tests.UI.Online;

public sealed class FolderReplayLibraryTests : IDisposable
{
    private static readonly DateTime _savedAt = new(2026, 10, 17, 14, 3, 22, DateTimeKind.Local);

    private readonly string _path = Path.Combine(Path.GetTempPath(), "fg-replay-library-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ListsWhoPlayedWhomAndMarksReplaysOfOtherVersionsUnsupported()
    {
        var folder = new ReplayFolder(_path);
        string playable = folder.Save(ReplayOf(GameVersion.Current, colour2: 2), _savedAt.AddMinutes(1), "local");
        string other = folder.Save(ReplayOf("0.9.0+other", colour2: 0), _savedAt, "local");
        var library = new FolderReplayLibrary(folder, () => TestGameData.Data);

        IReadOnlyList<ReplayEntry> entries = library.List();

        Assert.Equal(
            [
                new ReplayEntry(playable, _savedAt.AddMinutes(1), "Testman", 0, "Testman", 2, Rejection: null),
                new ReplayEntry(other, _savedAt, string.Empty, 0, string.Empty, 0, ReplayRejection.OtherGameVersion),
            ],
            entries);
    }

    [Fact]
    public void ADamagedFileIsListedAsUnplayable()
    {
        Directory.CreateDirectory(_path);
        string file = Path.Combine(_path, ReplayFolder.FileName(_savedAt, "local"));
        File.WriteAllBytes(file, [1, 2, 3]);

        ReplayEntry entry = Assert.Single(new FolderReplayLibrary(new ReplayFolder(_path), () => TestGameData.Data).List());

        Assert.False(entry.IsPlayable);
    }

    [Fact]
    public void DeletesTheEntrysFile()
    {
        var folder = new ReplayFolder(_path);
        folder.Save(ReplayOf(GameVersion.Current, colour2: 0), _savedAt, "local");
        var library = new FolderReplayLibrary(folder, () => TestGameData.Data);

        library.Delete(library.List()[0]);

        Assert.Empty(library.List());
    }

    public void Dispose()
    {
        if (Directory.Exists(_path))
        {
            Directory.Delete(_path, recursive: true);
        }
    }

    private static Replay ReplayOf(string gameVersion, byte colour2)
    {
        MatchRules rules = TestGameData.Data.CreateMatchRules();
        var header = new ReplayHeader(
            gameVersion,
            TestGameData.Data.Hash.ToBytes(),
            in rules,
            new ReplayPlayer("testman", 0),
            new ReplayPlayer("testman", colour2));
        return new Replay(header, [default(FrameInputs)]);
    }
}
