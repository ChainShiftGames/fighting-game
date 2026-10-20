using System;
using System.Collections.Generic;
using FightingGame.Loop;
using FightingGame.Online;
using Godot;
using Platform.Steam.Lobbies;

namespace FightingGame.UI;

/// <summary>
/// The placeholder menus (<see cref="MenuScreen"/>): main menu, online lobby, match over and pause. Shows the one that
/// fits what the game is doing and turns its buttons into calls on the <see cref="MatchLauncher"/>.
/// </summary>
/// <remarks>
/// Escape or a joypad's Start opens the pause menu during a match; B or Escape goes back a menu where there is one. On this PC it holds the match; online it can't
/// (the other player's game keeps running), so there it only offers to leave.
/// </remarks>
public partial class GameMenus : Node
{
    // Developer setting: how often the friend list is re-read from the platform.
    private const double FriendRefreshSeconds = 2.0;

    // Placeholder menu keys (developer settings until the menus, which will follow the player's bindings).
    private const Key PauseKey = Key.Escape;
    private const JoyButton PauseButton = JoyButton.Start;

    private readonly MenuPanel _panel = new();
    private MatchLauncher? _launcher;
    private FrameStepping? _stepping;
    private StartedMatch? _match;
    private bool _isPaused;
    private double _untilFriendRefresh;
    private string _lastAction = string.Empty;
    private LobbyStage _shownStage = LobbyStage.Idle;

    /// <summary>Whether a menu is on screen, so the host puts its match messages in it instead of over it.</summary>
    public bool IsOpen => _panel.Visible;

    public override void _Ready() => AddChild(_panel);

    /// <param name="stepping">Held while the pause menu is open over a match on this PC.</param>
    public void Bind(MatchLauncher launcher, FrameStepping stepping)
    {
        _launcher = launcher;
        _stepping = stepping;
    }

    /// <summary>Shows the menu that fits now, or none; call once per displayed frame.</summary>
    /// <param name="match">The match being played or just finished; null when there is none.</param>
    /// <param name="note">What the match (or a failed start) has to say, e.g. a desync; may be empty.</param>
    public void Refresh(StartedMatch? match, string note, double delta)
    {
        if (_launcher is null)
        {
            return;
        }

        _match = match;
        if (match is null || match.IsOver)
        {
            _isPaused = false;
        }

        MenuScreen screen = MenuScreenChoice.For(match is not null, match?.IsOver ?? false, _launcher.Mode, _isPaused);
        // A and B are also attack buttons: after the pause menu closes, the match waits until they are let go, so the
        // press that resumed it doesn't attack.
        bool isHeldAfterMenu = _stepping!.IsHeld && screen == MenuScreen.None && MenuPanel.IsMenuButtonHeld();
        _stepping.IsHeld = _launcher.IsLocal && (screen == MenuScreen.Pause || isHeldAfterMenu);
        LobbyFlow? lobby = _launcher.Matchmaking?.Lobby;
        FollowLobby(lobby, screen, delta);
        switch (screen)
        {
            case MenuScreen.MainMenu:
                _panel.Show("FIGHTING GAME", note, MainMenuEntries(), overMatch: false);
                break;
            case MenuScreen.Lobby:
                _panel.Show(
                    "ONLINE", LobbyMenuText.Describe(lobby!, _lastAction, note), LobbyEntries(lobby!), overMatch: false,
                    LobbyBack(lobby!));
                break;
            case MenuScreen.MatchOver when _launcher.Mode == LaunchMode.Online:
                _panel.Show("MATCH OVER", LobbyMenuText.Describe(lobby!, _lastAction, note), LobbyEntries(lobby!), overMatch: true);
                break;
            case MenuScreen.MatchOver:
                _panel.Show("MATCH OVER", note, MatchOverEntries(), overMatch: true);
                break;
            case MenuScreen.Pause:
                _panel.Show(_launcher.IsLocal ? "PAUSED" : "MENU", note, PauseEntries(), overMatch: true, () => _isPaused = false);
                break;
            default:
                _panel.Close();
                break;
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        bool pausePressed = inputEvent switch
        {
            InputEventKey { Pressed: true, Echo: false } key => key.PhysicalKeycode == PauseKey,
            InputEventJoypadButton { Pressed: true } button => button.ButtonIndex == PauseButton,
            _ => false,
        };
        if (!pausePressed || _match is null || _match.IsOver)
        {
            return;
        }

        _isPaused = !_isPaused;
        GetViewport().SetInputAsHandled();
    }

    private List<MenuEntry> MainMenuEntries()
    {
        bool hasOnline = _launcher!.Matchmaking is not null;
        return
        [
            new("Versus (2 players)", _launcher.StartVersus),
            new("Training", _launcher.StartTraining),
            new(hasOnline ? "Online" : $"Online (needs Steam: {_launcher.PlatformName})", () => _launcher.OpenOnline(), hasOnline),
            new("Quit", () => GetTree().Quit()),
        ];
    }

    private List<MenuEntry> MatchOverEntries()
    {
        var entries = new List<MenuEntry>();
        if (_launcher!.CanRematch)
        {
            entries.Add(new(_launcher.Mode == LaunchMode.Replay ? "Watch again" : "Rematch", _launcher.Rematch));
        }

        entries.Add(new("Main menu", _launcher.ReturnToMainMenu));
        return entries;
    }

    private List<MenuEntry> PauseEntries() =>
    [
        new("Resume", () => _isPaused = false),
        new(_launcher!.IsLocal ? "Main menu" : "Leave the match", _launcher.ReturnToMainMenu),
    ];

    // Back from the lobby menu: out of the lobby to the Online menu, from there to the main menu. Nothing once both
    // players are ready (the match is starting) or after a match (the menu's own buttons say where to go).
    private Action? LobbyBack(LobbyFlow lobby) => lobby.Stage switch
    {
        LobbyStage.Idle => _launcher!.ReturnToMainMenu,
        LobbyStage.Entering or LobbyStage.InLobby => lobby.Leave,
        _ => null,
    };

    private List<MenuEntry> LobbyEntries(LobbyFlow lobby)
    {
        var entries = new List<MenuEntry>();
        foreach (LobbyMenuItem item in LobbyMenuItems.For(lobby))
        {
            entries.Add(new MenuEntry(item.Label, () => Choose(lobby, item), item.Enabled));
        }

        return entries;
    }

    private void Choose(LobbyFlow lobby, LobbyMenuItem item)
    {
        switch (item.Choice)
        {
            case LobbyChoice.Host:
                lobby.Host();
                break;
            case LobbyChoice.JoinFriend:
                lobby.JoinFriend(item.FriendIndex);
                break;
            case LobbyChoice.InviteThroughOverlay:
                _lastAction = lobby.OpenInviteOverlay()
                    ? "Steam overlay open: pick a friend to invite."
                    : "No Steam overlay (start the game from Steam to get it): invite from the list instead.";
                break;
            case LobbyChoice.InviteFriend:
                string name = lobby.InvitableFriends[item.FriendIndex].Name;
                _lastAction = lobby.Invite(item.FriendIndex) ? $"Invite sent to {name}." : $"Couldn't invite {name}.";
                break;
            case LobbyChoice.Ready:
                _launcher!.Rematch();
                break;
            case LobbyChoice.Leave:
                lobby.Leave();
                break;
            case LobbyChoice.MainMenu:
                _launcher!.ReturnToMainMenu();
                break;
        }
    }

    // The friend list is re-read while it is shown (to join from, or invite); notes are for the stage they were made in.
    private void FollowLobby(LobbyFlow? lobby, MenuScreen screen, double delta)
    {
        if (lobby is null)
        {
            return;
        }

        if (lobby.Stage != _shownStage)
        {
            GD.Print($"Lobby: {_shownStage} → {lobby.Stage}.");
            _shownStage = lobby.Stage;
            _lastAction = string.Empty;
        }

        _untilFriendRefresh -= delta;
        bool listsFriends = lobby.Stage == LobbyStage.Idle || (lobby.Stage == LobbyStage.InLobby && !lobby.HasOpponent);
        if (screen == MenuScreen.Lobby && listsFriends && _untilFriendRefresh <= 0)
        {
            _untilFriendRefresh = FriendRefreshSeconds;
            lobby.RefreshFriends();
        }
    }
}
