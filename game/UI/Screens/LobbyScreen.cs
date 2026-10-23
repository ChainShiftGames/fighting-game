using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Framework;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Screens;

/// <summary>
/// The online lobby: host, join or invite, ready. After an online match the same screen is shown over it, offering the
/// rematch (<see cref="IsOverMatch"/>).
/// </summary>
public sealed class LobbyScreen : IMenuScreen
{
    // Developer setting: how often the friend list is re-read from the platform.
    private const double FriendRefreshSeconds = 2.0;

    private readonly MatchLauncher _launcher;
    private readonly LobbyFlow _lobby;
    private readonly Func<string> _note;
    private readonly Action<string> _log;
    private MenuText _lastAction = MenuText.Empty;
    private LobbyStage _shownStage = LobbyStage.Idle;
    private double _untilFriendRefresh;

    /// <param name="note">What the match underneath (or a failed start) has to say; may be empty.</param>
    /// <param name="log">Writes a line to the game's log (stage changes).</param>
    public LobbyScreen(MatchLauncher launcher, LobbyFlow lobby, Func<string> note, Action<string> log)
    {
        _launcher = launcher;
        _lobby = lobby;
        _note = note;
        _log = log;
    }

    public MenuText Title => MenuText.Key(IsOverMatch ? "MENU_TITLE_MATCH_OVER" : "LOBBY_TITLE");

    public IReadOnlyList<MenuText> Body => LobbyMenuText.Describe(_lobby, _lastAction, _note());

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>();
            foreach (LobbyMenuItem item in LobbyMenuItems.For(_lobby))
            {
                items.Add(new ButtonItem(item.Label, () => Choose(item), item.Enabled));
            }

            return items;
        }
    }

    /// <summary>Over the online match that just finished, rather than as a full screen.</summary>
    public bool IsOverMatch { get; set; }

    // Out of the lobby to the Online menu, from there to the main menu. Nothing once both players are ready (the
    // match is starting) or after a match (the menu's own buttons say where to go). The launcher's state then picks
    // the screen, so the stack never pops this one itself.
    public bool Back()
    {
        switch (_lobby.Stage)
        {
            case LobbyStage.Idle:
                _launcher.ReturnToMainMenu();
                break;
            case LobbyStage.Entering or LobbyStage.InLobby:
                _lobby.Leave();
                break;
        }

        return false;
    }

    public void Press(MenuPress press)
    {
    }

    /// <summary>
    /// Call once per displayed frame: re-reads the friend list while it is shown (to join from, or invite); notes are
    /// for the stage they were made in.
    /// </summary>
    public void Follow(bool isShown, double delta)
    {
        if (_lobby.Stage != _shownStage)
        {
            _log($"Lobby: {_shownStage} → {_lobby.Stage}.");
            _shownStage = _lobby.Stage;
            _lastAction = MenuText.Empty;
        }

        _untilFriendRefresh -= delta;
        bool listsFriends =
            _lobby.Stage == LobbyStage.Idle || (_lobby.Stage == LobbyStage.InLobby && !_lobby.HasOpponent);
        if (isShown && listsFriends && _untilFriendRefresh <= 0)
        {
            _untilFriendRefresh = FriendRefreshSeconds;
            _lobby.RefreshFriends();
        }
    }

    private void Choose(LobbyMenuItem item)
    {
        switch (item.Choice)
        {
            case LobbyChoice.Host:
                _lobby.Host();
                break;
            case LobbyChoice.JoinFriend:
                _lobby.JoinFriend(item.FriendIndex);
                break;
            case LobbyChoice.InviteThroughOverlay:
                _lastAction = MenuText.Key(_lobby.OpenInviteOverlay() ? "LOBBY_OVERLAY_OPEN" : "LOBBY_NO_OVERLAY");
                break;
            case LobbyChoice.InviteFriend:
                string name = _lobby.InvitableFriends[item.FriendIndex].Name;
                string result = _lobby.Invite(item.FriendIndex) ? "LOBBY_INVITE_SENT" : "LOBBY_INVITE_FAILED";
                _lastAction = MenuText.Key(result, name);
                break;
            case LobbyChoice.Ready:
                _launcher.Rematch();
                break;
            case LobbyChoice.Leave:
                _lobby.Leave();
                break;
            case LobbyChoice.MainMenu:
                _launcher.ReturnToMainMenu();
                break;
        }
    }
}
