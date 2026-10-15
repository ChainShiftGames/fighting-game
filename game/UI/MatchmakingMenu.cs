using System.Text;
using FightingGame.Platform;
using Godot;

namespace FightingGame.UI;

/// <summary>
/// Placeholder online menu: lists friends running the game, number keys invite them. Shown until the match starts.
/// </summary>
public partial class MatchmakingMenu : CanvasLayer
{
    // Developer setting: how often the friend list is re-read from the platform.
    private const double FriendRefreshSeconds = 2.0;
    private const int MaxListedFriends = 9;

    private readonly Label _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AnchorRight = 1f,
        AnchorBottom = 1f,
    };

    private IMatchmaking? _matchmaking;
    private double _untilRefresh;
    private string _lastAction = string.Empty;
    private bool _loggedReady;

    public override void _Ready() => AddChild(_label);

    public void Show(IMatchmaking matchmaking)
    {
        _matchmaking = matchmaking;
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (_matchmaking is null || !Visible)
        {
            return;
        }

        if (_matchmaking.IsReady && !_loggedReady)
        {
            _loggedReady = true;
            GD.Print("Online lobby open: waiting for an invite or a friend to invite.");
        }

        _untilRefresh -= delta;
        if (_untilRefresh <= 0)
        {
            _untilRefresh = FriendRefreshSeconds;
            _matchmaking.RefreshFriends();
        }

        _label.Text = Describe(_matchmaking);
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (_matchmaking is null || !Visible || inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        int index = (int)key.PhysicalKeycode - (int)Key.Key1;
        if (index >= 0 && index < MaxListedFriends && index < _matchmaking.InvitableFriends.Count)
        {
            _matchmaking.Invite(index);
            _lastAction = $"Invite sent to {_matchmaking.InvitableFriends[index]}.";
        }
    }

    private string Describe(IMatchmaking matchmaking)
    {
        if (!matchmaking.IsReady)
        {
            return "Opening an online lobby…";
        }

        var text = new StringBuilder("Online: waiting for an opponent\n\n");
        if (matchmaking.InvitableFriends.Count == 0)
        {
            text.Append("No friends are running the game right now.\n");
        }
        else
        {
            text.Append("Press a number to invite a friend who is running the game:\n");
            for (int i = 0; i < matchmaking.InvitableFriends.Count && i < MaxListedFriends; i++)
            {
                text.Append($"{i + 1}   {matchmaking.InvitableFriends[i]}\n");
            }
        }

        text.Append("\nOr accept a friend's invite in Steam.\n").Append(_lastAction);
        return text.ToString();
    }
}
