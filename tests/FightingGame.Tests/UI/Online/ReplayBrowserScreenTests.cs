using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FightingGame.Tests.UI.Flow;
using FightingGame.Tests.UI.Framework;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.Online;
using Netcode.Replays;
using Xunit;

namespace FightingGame.Tests.UI.Online;

public sealed class ReplayBrowserScreenTests
{
    private static readonly DateTime _savedAt = new(2026, 10, 17, 14, 3, 22, DateTimeKind.Local);
    private static readonly ReplayEntry _playable = new("a.fgreplay", _savedAt, "Testman", 0, "Ken", 2, Rejection: null);
    private static readonly ReplayEntry _unsupported =
        new("b.fgreplay", _savedAt.AddDays(-1), string.Empty, 0, string.Empty, 0, ReplayRejection.OtherGameVersion);

    private readonly RecordingMatchStarter _matches = new();
    private readonly TestLibrary _library = new();
    private readonly GameFlow _flow;
    private readonly ReplayBrowserScreen _screen;

    public ReplayBrowserScreenTests()
    {
        _flow = new GameFlow(_matches, step => step is FlowStep.Title or FlowStep.MainMenu or FlowStep.Replays);
        _screen = new ReplayBrowserScreen(_flow, _library);
        _flow.PressOnTitle(MenuDevice.Keyboard);
        _flow.Open(FlowStep.Replays);
        _library.Entries.AddRange([_playable, _unsupported]);
        _screen.Follow(isShown: true);
    }

    [Fact]
    public void ListsEachReplayWithDateCharactersAndColoursAndOldOnesAsUnsupported()
    {
        Assert.Equal(
            ["2026-10-17 14:03   Testman (colour 1) vs Ken (colour 3)", "2026-10-16 14:03   unsupported", "Back"],
            Labels(_screen));
        Assert.Equal(
            "Recorded with another version of the game, so it can't be played here.",
            MenuTextTable.Formatter.Format(_screen.Items[1].Description));
    }

    [Fact]
    public void AChosenReplayCanBeWatched()
    {
        Choose(_screen, 0);
        IMenuScreen entry = _screen.Over!;

        Choose(entry, 0);

        Assert.Equal(["a.fgreplay"], _matches.Watched);
        Assert.Equal(FlowStep.Match, _flow.Step);
    }

    [Fact]
    public void AnUnsupportedReplayCantBeWatchedButCanBeDeleted()
    {
        Choose(_screen, 1);
        IMenuScreen entry = _screen.Over!;

        Assert.False(entry.Items[0].Enabled);
        Assert.True(entry.Items[1].Enabled);
    }

    [Fact]
    public void DeletingAsksFirstWithTheCursorOnCancel()
    {
        Choose(_screen, 0);
        Choose(_screen.Over!, 1);
        IMenuScreen question = _screen.Over!;
        Assert.Equal(1, question.FirstFocus);

        Choose(question, 0);

        Assert.Null(_screen.Over);
        Assert.Equal([_unsupported], _library.Entries);
        Assert.Equal(["2026-10-16 14:03   unsupported", "Back"], Labels(_screen));
    }

    [Fact]
    public void CancelAndBackReturnToTheListThenTheMainMenu()
    {
        Choose(_screen, 0);
        Choose(_screen.Over!, 1);
        _screen.Over!.Back();
        Assert.Null(_screen.Over);
        Assert.Equal(2, _library.Entries.Count);

        _screen.Back();

        Assert.Equal(FlowStep.MainMenu, _flow.Step);
    }

    [Fact]
    public void ALongListShowsAPageAtATime()
    {
        _library.Entries.Clear();
        for (int i = 0; i < (2 * ReplayBrowserScreen.PageSize) + 1; i++)
        {
            _library.Entries.Add(_playable with { Path = $"{i}.fgreplay", SavedAt = _savedAt.AddMinutes(-i) });
        }

        _screen.Follow(isShown: false);
        _screen.Follow(isShown: true);
        Assert.Equal(ReplayBrowserScreen.PageSize + 2, _screen.Items.Count);
        Assert.EndsWith("1–4 of 9", MenuTextTable.Formatter.FormatLines(_screen.Body), StringComparison.Ordinal);
        Assert.Equal(["Older replays", "Back"], Labels(_screen)[^2..]);

        Choose(_screen, ReplayBrowserScreen.PageSize);
        Choose(_screen, ReplayBrowserScreen.PageSize);
        Assert.Equal(["2026-10-17 13:55   Testman (colour 1) vs Ken (colour 3)", "Newer replays", "Back"], Labels(_screen));

        // Deleting the only replay on the last page goes back a page.
        Choose(_screen, 0);
        Choose(_screen.Over!, 1);
        Choose(_screen.Over!, 0);
        Assert.EndsWith("5–8 of 8", MenuTextTable.Formatter.FormatLines(_screen.Body), StringComparison.Ordinal);
    }

    [Fact]
    public void AFolderThatCantBeReadSaysWhy()
    {
        _library.Failure = new IOException("Access denied.");
        _screen.Follow(isShown: false);
        _screen.Follow(isShown: true);

        Assert.Equal("Access denied.", MenuTextTable.Formatter.FormatLines(_screen.Body));
        Assert.Equal(["Back"], Labels(_screen));
    }

    private static string[] Labels(IMenuScreen screen) =>
        [.. screen.Items.Select(item => MenuTextTable.Formatter.Format(item.Label))];

    private static void Choose(IMenuScreen screen, int index) => screen.Items[index].Act(MenuAction.Confirm);

    private sealed class TestLibrary : IReplayLibrary
    {
        public List<ReplayEntry> Entries { get; } = [];

        public IOException? Failure { get; set; }

        public IReadOnlyList<ReplayEntry> List() => Failure is null ? [.. Entries] : throw Failure;

        public void Delete(ReplayEntry entry) => Entries.Remove(entry);
    }
}
