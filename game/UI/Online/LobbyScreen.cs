using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Online;

/// <summary>
/// The online screens: the online menu (host, join a friend), then the lobby (both players, their readiness, the
/// connection, invites, leave). Once both are ready it moves the flow on to character select, and back here when the
/// lobby drops out of picking (the opponent left).
/// </summary>
public sealed class LobbyScreen : IMenuScreen
{
    // Developer setting: how often the friend list is re-read from the platform.
    private const double FriendRefreshSeconds = 2.0;

    private readonly GameFlow _flow;
    private readonly MatchLauncher _launcher;
    private readonly LobbyFlow _lobby;
    private readonly string _playerName;
    private readonly Func<string> _note;
    private readonly Action<string> _log;
    private MenuText _lastAction = MenuText.Empty;
    private LobbyStage _shownStage = LobbyStage.Idle;
    private double _untilFriendRefresh;

    /// <param name="playerName">This player's name on the platform.</param>
    /// <param name="note">What a failed match start has to say; may be empty.</param>
    /// <param name="log">Writes a line to the game's log (stage changes).</param>
    public LobbyScreen(
        GameFlow flow, MatchLauncher launcher, LobbyFlow lobby, string playerName, Func<string> note, Action<string> log)
    {
        _flow = flow;
        _launcher = launcher;
        _lobby = lobby;
        _playerName = playerName;
        _note = note;
        _log = log;
    }

    public MenuText Title => MenuText.Key(_lobby.Stage == LobbyStage.Idle ? "LOBBY_TITLE" : "LOBBY_TITLE_LOBBY");

    public IReadOnlyList<MenuText> Body => LobbyMenuText.Describe(_lobby, _playerName, _lastAction, _note());

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>();
            foreach (LobbyMenuItem item in LobbyMenuItems.For(_lobby))
            {
                items.Add(new ButtonItem(item.Label, () => Choose(item), item.Enabled) { Description = item.Description });
            }

            return items;
        }
    }

    public bool IsOverMatch => false;

    // Out of the lobby to the online menu, from there to the main menu. Nothing once both players are ready (the
    // match is starting). The flow then picks the screen, so the stack never pops this one itself.
    public bool Back()
    {
        switch (_lobby.Stage)
        {
            case LobbyStage.Idle:
                _flow.Back();
                break;
            case LobbyStage.Entering or LobbyStage.InLobby or LobbyStage.Rematch:
                _lobby.Leave();
                break;
        }

        return false;
    }

    public void Press(MenuPress press)
    {
    }

    /// <summary>
    /// Call once per displayed frame: follows the lobby with the flow (on to character select, back from it), re-reads
    /// the friend list while it is shown (to join from, or invite); notes are for the stage they were made in.
    /// </summary>
    public void Follow(bool isShown, double delta)
    {
        if (_lobby.Stage != _shownStage)
        {
            _log($"Lobby: {_shownStage} → {_lobby.Stage}.");
            _shownStage = _lobby.Stage;
            _lastAction = MenuText.Empty;
        }

        FollowWithFlow();
        _untilFriendRefresh -= delta;
        bool listsFriends =
            _lobby.Stage == LobbyStage.Idle || (_lobby.Stage == LobbyStage.InLobby && !_lobby.HasOpponent);
        if (isShown && listsFriends && _untilFriendRefresh <= 0)
        {
            _untilFriendRefresh = FriendRefreshSeconds;
            _lobby.RefreshFriends();
        }
    }

    private void FollowWithFlow()
    {
        if (_flow.Setup.Mode != LaunchMode.Online)
        {
            return;
        }

        bool isPicking = _lobby.Stage is LobbyStage.Picking or LobbyStage.Starting;
        if (_flow.Step == FlowStep.Online && isPicking)
        {
            _flow.Advance(_flow.Setup);
        }
        else if (_flow.Step == FlowStep.CharacterSelect && !isPicking && _lobby.Stage != LobbyStage.InMatch)
        {
            _flow.Back();
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
                _flow.ToMainMenu();
                break;
            case LobbyChoice.QuickMatch:
                break;
        }
    }
}
