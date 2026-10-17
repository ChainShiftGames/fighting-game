using System.Collections.Generic;
using System.Globalization;
using Platform.Steam.Friends;
using Steamworks;

namespace Platform.Steam.Lobbies;

/// <summary>
/// <see cref="ILobby"/> over Steamworks.NET: a friends-only two-player Steam lobby, ready marks as lobby member data.
/// </summary>
/// <remarks>Steam delivers the callbacks below from <see cref="SteamSession.RunCallbacks"/>.</remarks>
public sealed class SteamLobby : ILobby
{
    private const int MaxMembers = 2;
    private const string ReadyForKey = "ready_for";
    private const int NotReady = 0;

    private readonly Callback<LobbyCreated_t> _created;
    private readonly Callback<LobbyEnter_t> _entered;
    private readonly Callback<GameLobbyJoinRequested_t> _inviteAccepted;
    private CSteamID _lobby = CSteamID.Nil;
    private CSteamID _joining = CSteamID.Nil;
    private bool _creating;
    private ulong? _acceptedInvite;
    private string _failure = string.Empty;

    internal SteamLobby()
    {
        _created = Callback<LobbyCreated_t>.Create(OnCreated);
        _entered = Callback<LobbyEnter_t>.Create(OnEntered);
        _inviteAccepted = Callback<GameLobbyJoinRequested_t>.Create(OnInviteAccepted);
    }

    public LobbyPresence Presence =>
        _lobby.IsValid() ? LobbyPresence.Inside
        : _creating || _joining.IsValid() ? LobbyPresence.Entering
        : LobbyPresence.Outside;

    public bool IsOwner => _lobby.IsValid() && SteamMatchmaking.GetLobbyOwner(_lobby) == SteamUser.GetSteamID();

    public SteamFriend? Opponent => OpponentId() is { } opponent
        ? new SteamFriend(opponent.m_SteamID, SteamFriends.GetFriendPersonaName(opponent))
        : null;

    public int OpponentReadyFor =>
        OpponentId() is { } opponent &&
        int.TryParse(
            SteamMatchmaking.GetLobbyMemberData(_lobby, opponent, ReadyForKey),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int matchNumber)
            ? matchNumber
            : NotReady;

    public ulong? TakeAcceptedInvite()
    {
        ulong? invite = _acceptedInvite;
        _acceptedInvite = null;
        return invite;
    }

    public string TakeFailure()
    {
        string failure = _failure;
        _failure = string.Empty;
        return failure;
    }

    public void Create()
    {
        _creating = true;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxMembers);
    }

    public void Join(ulong lobbyId)
    {
        _joining = new CSteamID(lobbyId);
        SteamMatchmaking.JoinLobby(_joining);
    }

    public void Leave()
    {
        if (_lobby.IsValid())
        {
            SteamMatchmaking.LeaveLobby(_lobby);
        }

        _lobby = CSteamID.Nil;
        _joining = CSteamID.Nil;
        _creating = false;
    }

    public void MarkReadyFor(int matchNumber)
    {
        if (_lobby.IsValid())
        {
            SteamMatchmaking.SetLobbyMemberData(_lobby, ReadyForKey, matchNumber.ToString(CultureInfo.InvariantCulture));
        }
    }

    public bool Invite(ulong friendSteamId) =>
        _lobby.IsValid() && SteamMatchmaking.InviteUserToLobby(_lobby, new CSteamID(friendSteamId));

    // The overlay only exists when Steam injected it at launch (games started from the Steam client).
    public bool OpenInviteOverlay()
    {
        if (!_lobby.IsValid() || !SteamUtils.IsOverlayEnabled())
        {
            return false;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(_lobby);
        return true;
    }

    public IReadOnlyList<SteamFriend> FriendsPlayingThisGame() => FriendsInGame.List();

    public void Dispose()
    {
        Leave();
        _created.Dispose();
        _entered.Dispose();
        _inviteAccepted.Dispose();
    }

    private CSteamID? OpponentId()
    {
        if (!_lobby.IsValid())
        {
            return null;
        }

        CSteamID self = SteamUser.GetSteamID();
        int members = SteamMatchmaking.GetNumLobbyMembers(_lobby);
        for (int i = 0; i < members; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i);
            if (member != self)
            {
                return member;
            }
        }

        return null;
    }

    private void OnCreated(LobbyCreated_t created)
    {
        if (created.m_eResult == EResult.k_EResultOK)
        {
            // Steam enters a created lobby by itself (LobbyEnter_t follows); one no longer wanted is left again.
            if (!_creating)
            {
                SteamMatchmaking.LeaveLobby(new CSteamID(created.m_ulSteamIDLobby));
            }

            return;
        }

        if (_creating)
        {
            _creating = false;
            _failure = $"Couldn't create a lobby ({created.m_eResult}).";
        }
    }

    private void OnEntered(LobbyEnter_t entered)
    {
        var lobby = new CSteamID(entered.m_ulSteamIDLobby);
        bool wanted = lobby == _joining || (_creating && !_joining.IsValid());
        var response = (EChatRoomEnterResponse)entered.m_EChatRoomEnterResponse;
        if (response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            if (wanted)
            {
                _failure = $"Couldn't join the lobby ({response}).";
                _joining = CSteamID.Nil;
                _creating = false;
            }

            return;
        }

        if (!wanted)
        {
            SteamMatchmaking.LeaveLobby(lobby);
            return;
        }

        _lobby = lobby;
        _joining = CSteamID.Nil;
        _creating = false;
    }

    // An invite to the lobby this PC is already in changes nothing.
    private void OnInviteAccepted(GameLobbyJoinRequested_t invite)
    {
        if (invite.m_steamIDLobby != _lobby)
        {
            _acceptedInvite = invite.m_steamIDLobby.m_SteamID;
        }
    }
}
