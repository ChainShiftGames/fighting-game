using System;
using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Online;

/// <summary>Asks before a replay is deleted for good. The cursor starts on cancel, so a mashed confirm keeps it.</summary>
public sealed class DeleteReplayScreen : IMenuScreen
{
    private readonly ReplayEntry _entry;
    private readonly Action _delete;
    private readonly Action _cancel;

    public DeleteReplayScreen(ReplayEntry entry, Action delete, Action cancel)
    {
        _entry = entry;
        _delete = delete;
        _cancel = cancel;
    }

    public MenuText Title { get; } = MenuText.Key("REPLAY_TITLE_DELETE");

    public IReadOnlyList<MenuText> Body => [ReplayEntryText.Label(_entry), MenuText.Empty, MenuText.Key("REPLAY_DELETE_BODY")];

    public IReadOnlyList<MenuItem> Items =>
    [
        new ButtonItem(MenuText.Key("REPLAY_DELETE"), _delete),
        new ButtonItem(MenuText.Key("MENU_CANCEL"), _cancel),
    ];

    public int FirstFocus => 1;

    public bool IsOverMatch => false;

    public bool Back()
    {
        _cancel();
        return false;
    }

    public void Press(MenuPress press)
    {
    }
}
