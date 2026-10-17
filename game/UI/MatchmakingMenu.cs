using FightingGame.Platform;
using Godot;
using Platform.Steam.Lobbies;

namespace FightingGame.UI;

/// <summary>
/// Placeholder lobby menu (keys only, until the real menus): host, invite, ready, rematch, leave. Hidden while a
/// match is being played; shown again over the finished match to offer a rematch.
/// </summary>
public partial class MatchmakingMenu : CanvasLayer
{
    // Developer setting: how often the friend list is re-read from the platform.
    private const double FriendRefreshSeconds = 2.0;

    // Placeholder menu keys (developer settings until the menus, which will follow the player's bindings).
    private const Key HostKey = Key.H;
    private const Key ReadyKey = Key.Enter;
    private const Key InviteOverlayKey = Key.I;
    private const Key LeaveKey = Key.Escape;

    private readonly Label _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AnchorRight = 1f,
        AnchorBottom = 1f,
    };

    private LobbyFlow? _lobby;
    private double _untilRefresh;
    private string _lastAction = string.Empty;
    private LobbyStage _shownStage = LobbyStage.Idle;

    public override void _Ready() => AddChild(_label);

    public void Show(IMatchmaking matchmaking)
    {
        _lobby = matchmaking.Lobby;
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (_lobby is null)
        {
            return;
        }

        if (_lobby.Stage != _shownStage)
        {
            GD.Print($"Lobby: {_shownStage} → {_lobby.Stage}.");
            _shownStage = _lobby.Stage;
            _lastAction = string.Empty;
        }

        Visible = _lobby.Stage != LobbyStage.InMatch;
        _untilRefresh -= delta;
        if (_untilRefresh <= 0 && _lobby.Stage == LobbyStage.InLobby && !_lobby.HasOpponent)
        {
            _untilRefresh = FriendRefreshSeconds;
            _lobby.RefreshFriends();
        }

        _label.Text = LobbyMenuText.Describe(_lobby, _lastAction, MatchNote);
    }

    /// <summary>What the match underneath says while the menu covers it (e.g. a desync); empty for nothing.</summary>
    public string MatchNote { get; set; } = string.Empty;

    // No keys during a match: they belong to the fight.
    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (_lobby is null || _lobby.Stage == LobbyStage.InMatch ||
            inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        Key pressed = key.PhysicalKeycode;
        if (pressed == HostKey)
        {
            _lobby.Host();
        }
        else if (pressed == ReadyKey)
        {
            _lobby.Ready();
        }
        else if (pressed == LeaveKey)
        {
            _lobby.Leave();
        }
        else if (pressed == InviteOverlayKey)
        {
            _lastAction = _lobby.OpenInviteOverlay()
                ? "Steam overlay open: pick a friend to invite."
                : "No Steam overlay (start the game from Steam to get it): invite from the list instead.";
        }
        else
        {
            InviteListedFriend((int)pressed - (int)Key.Key1);
        }
    }

    private void InviteListedFriend(int index)
    {
        if (index < 0 || index >= LobbyMenuText.MaxListedFriends || index >= _lobby!.InvitableFriends.Count)
        {
            return;
        }

        string name = _lobby.InvitableFriends[index].Name;
        _lastAction = _lobby.Invite(index) ? $"Invite sent to {name}." : $"Couldn't invite {name}.";
    }
}
