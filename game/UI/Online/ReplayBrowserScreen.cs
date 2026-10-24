using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using Sim.Data.Validation;

namespace FightingGame.UI.Online;

/// <summary>
/// The replay browser: saved replays newest first (date, characters, colours), a page at a time. Choosing one opens it
/// to watch or delete (<see cref="Over"/>); a replay from another game version is listed as unsupported and can't be
/// watched (ADR-0018).
/// </summary>
/// <remarks>Pages because menu lists don't scroll: a page always fits on screen.</remarks>
public sealed class ReplayBrowserScreen : IMenuScreen
{
    /// <summary>Replays on one page (developer setting): as many as fit under the title with the page buttons.</summary>
    public const int PageSize = 4;

    private readonly GameFlow _flow;
    private readonly IReplayLibrary _library;
    private IReadOnlyList<ReplayEntry> _entries = [];
    private MenuText _problem = MenuText.Empty;
    private int _page;
    private bool _isOpen;

    public ReplayBrowserScreen(GameFlow flow, IReplayLibrary library)
    {
        _flow = flow;
        _library = library;
    }

    public MenuText Title { get; } = MenuText.Key("REPLAY_TITLE");

    public IReadOnlyList<MenuText> Body
    {
        get
        {
            if (!_problem.IsEmpty)
            {
                return [_problem];
            }

            if (_entries.Count == 0)
            {
                return [MenuText.Key("REPLAY_NONE")];
            }

            int first = _page * PageSize;
            return
            [
                MenuText.Key("REPLAY_HINT"),
                MenuText.Key(
                    "REPLAY_PAGE",
                    (first + 1).ToString(CultureInfo.InvariantCulture),
                    Math.Min(first + PageSize, _entries.Count).ToString(CultureInfo.InvariantCulture),
                    _entries.Count.ToString(CultureInfo.InvariantCulture)),
            ];
        }
    }

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>();
            int first = _page * PageSize;
            for (int i = first; i < _entries.Count && i < first + PageSize; i++)
            {
                ReplayEntry entry = _entries[i];
                items.Add(new ButtonItem(ReplayEntryText.Label(entry), () => Choose(entry))
                {
                    Description = entry.IsPlayable ? MenuText.Empty : ReplayEntryText.Reason(entry),
                });
            }

            if (_page < LastPage)
            {
                items.Add(new ButtonItem(MenuText.Key("REPLAY_OLDER"), () => _page++));
            }

            if (_page > 0)
            {
                items.Add(new ButtonItem(MenuText.Key("REPLAY_NEWER"), () => _page--));
            }

            items.Add(new ButtonItem(MenuText.Key("MENU_BACK"), _flow.Back));
            return items;
        }
    }

    public bool IsOverMatch => false;

    /// <summary>The replay chosen, or the question whether to delete it, shown over the list; null for none.</summary>
    public IMenuScreen? Over { get; private set; }

    private int LastPage => Math.Max(0, (_entries.Count - 1) / PageSize);

    public bool Back()
    {
        _flow.Back();
        return false;
    }

    public void Press(MenuPress press)
    {
    }

    /// <summary>Reads the replays again each time the browser opens; call once per displayed frame.</summary>
    public void Follow(bool isShown)
    {
        if (!isShown)
        {
            _isOpen = false;
            Over = null;
            return;
        }

        if (!_isOpen)
        {
            _isOpen = true;
            _page = 0;
            Reload();
        }
    }

    private void Choose(ReplayEntry entry) =>
        Over = new ReplayEntryScreen(entry, () => _flow.Watch(entry.Path), () => AskToDelete(entry), Close);

    private void AskToDelete(ReplayEntry entry) => Over = new DeleteReplayScreen(entry, () => Delete(entry), Close);

    private void Close() => Over = null;

    private void Delete(ReplayEntry entry)
    {
        Over = null;
        try
        {
            _library.Delete(entry);
            Reload();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _problem = MenuText.Key("REPLAY_DELETE_FAILED", exception.Message);
        }
    }

    // A folder or data that can't be read is said on screen; only back works then.
    private void Reload()
    {
        try
        {
            _entries = _library.List();
            _problem = MenuText.Empty;

            // A delete can empty the last page.
            _page = Math.Min(_page, LastPage);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DataValidationException or InvalidDataException)
        {
            _entries = [];
            _problem = MenuText.Plain(exception.Message);
        }
    }
}
