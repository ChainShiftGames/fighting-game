using System.Collections.Generic;
using Platform.Steam;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;
using Platform.Steam.Transport;

namespace FightingGame.Platform;

/// <summary>Matchmaking through a Steam lobby and friend invites, over Steam's relay (no IP addresses).</summary>
public sealed class SteamLobbyMatchmaking : IMatchmaking
{
    private readonly SteamSession _session;
    private readonly SteamLobby _lobby;
    private IReadOnlyList<SteamFriend> _friends = [];
    private readonly List<string> _friendNames = [];
    private MatchmakingResult? _result;

    public SteamLobbyMatchmaking(SteamSession session)
    {
        _session = session;
        _lobby = session.OpenLobby();
    }

    public bool IsReady => _lobby.IsOpen;

    public IReadOnlyList<string> InvitableFriends => _friendNames;

    public MatchmakingResult? Result
    {
        get
        {
            if (_result is null && _lobby.OpponentSteamId is { } opponent)
            {
                _result = new MatchmakingResult(
                    new SteamPeerEndPoint(opponent), _lobby.OpponentName, _lobby.IsOwner, SteamTransport.For(opponent));
            }

            return _result;
        }
    }

    public void RefreshFriends()
    {
        _friends = _session.FriendsPlayingThisGame();
        _friendNames.Clear();
        foreach (SteamFriend friend in _friends)
        {
            _friendNames.Add(friend.Name);
        }
    }

    public void Invite(int friendIndex)
    {
        if (friendIndex >= 0 && friendIndex < _friends.Count)
        {
            _lobby.Invite(_friends[friendIndex].SteamId);
        }
    }

    public void Dispose() => _lobby.Dispose();
}
