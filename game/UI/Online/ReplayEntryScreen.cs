using System;
using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Online;

/// <summary>One replay over the replay browser: watch it (when this build can), delete it, or back to the list.</summary>
public sealed class ReplayEntryScreen : IMenuScreen
{
    private readonly ReplayEntry _entry;
    private readonly Action _watch;
    private readonly Action _delete;
    private readonly Action _close;

    /// <param name="delete">Asks whether to delete it.</param>
    /// <param name="close">Back to the list.</param>
    public ReplayEntryScreen(ReplayEntry entry, Action watch, Action delete, Action close)
    {
        _entry = entry;
        _watch = watch;
        _delete = delete;
        _close = close;
    }

    public MenuText Title { get; } = MenuText.Key("REPLAY_TITLE_ENTRY");

    public IReadOnlyList<MenuText> Body => _entry.IsPlayable
        ? [ReplayEntryText.Label(_entry)]
        : [ReplayEntryText.Label(_entry), ReplayEntryText.Reason(_entry)];

    public IReadOnlyList<MenuItem> Items =>
    [
        new ButtonItem(MenuText.Key("REPLAY_WATCH"), _watch, _entry.IsPlayable),
        new ButtonItem(MenuText.Key("REPLAY_DELETE"), _delete),
        new ButtonItem(MenuText.Key("MENU_BACK"), _close),
    ];

    public bool IsOverMatch => false;

    public bool Back()
    {
        _close();
        return false;
    }

    public void Press(MenuPress press)
    {
    }
}
