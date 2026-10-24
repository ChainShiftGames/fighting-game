using System;
using System.Collections.Generic;
using System.IO;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using Sim.Data.Validation;

namespace FightingGame.UI.Online;

/// <summary>
/// The replay browser: saved replays newest first (date, characters, colours). Choosing one opens it to watch or delete
/// (<see cref="Over"/>); a replay from another game version is listed as unsupported and can't be watched (ADR-0018).
/// </summary>
public sealed class ReplayBrowserScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly IReplayLibrary _library;
    private IReadOnlyList<ReplayEntry> _entries = [];
    private MenuText _problem = MenuText.Empty;
    private bool _isOpen;

    public ReplayBrowserScreen(GameFlow flow, IReplayLibrary library)
    {
        _flow = flow;
        _library = library;
    }

    public MenuText Title { get; } = MenuText.Key("REPLAY_TITLE");

    public IReadOnlyList<MenuText> Body =>
        !_problem.IsEmpty ? [_problem] : [MenuText.Key(_entries.Count == 0 ? "REPLAY_NONE" : "REPLAY_HINT")];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>();
            foreach (ReplayEntry entry in _entries)
            {
                items.Add(new ButtonItem(ReplayEntryText.Label(entry), () => Choose(entry))
                {
                    Description = entry.IsPlayable ? MenuText.Empty : ReplayEntryText.Reason(entry),
                });
            }

            items.Add(new ButtonItem(MenuText.Key("MENU_BACK"), _flow.Back));
            return items;
        }
    }

    public bool IsOverMatch => false;

    /// <summary>The replay chosen, or the question whether to delete it, shown over the list; null for none.</summary>
    public IMenuScreen? Over { get; private set; }

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
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DataValidationException or InvalidDataException)
        {
            _entries = [];
            _problem = MenuText.Plain(exception.Message);
        }
    }
}
