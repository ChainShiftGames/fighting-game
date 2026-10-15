using System;
using Steamworks;

namespace Platform.Steam.Lobbies;

/// <summary>
/// A friends-only two-player Steam lobby. Each player opens one; inviting a friend lets them join it, and accepting
/// a friend's invite (in the Steam client) leaves this lobby for theirs. The lobby owner plays P1.
/// </summary>
/// <remarks>Steam delivers the callbacks below from <see cref="SteamSession.RunCallbacks"/>.</remarks>
public sealed class SteamLobby : IDisposable
{
    private const int MaxMembers = 2;

    private readonly Callback<LobbyCreated_t> _created;
    private readonly Callback<LobbyEnter_t> _entered;
    private readonly Callback<LobbyChatUpdate_t> _membersChanged;
    private readonly Callback<GameLobbyJoinRequested_t> _inviteAccepted;
    private CSteamID _lobby = CSteamID.Nil;
    private bool _joiningFriendsLobby;

    private SteamLobby()
    {
        _created = Callback<LobbyCreated_t>.Create(OnCreated);
        _entered = Callback<LobbyEnter_t>.Create(OnEntered);
        _membersChanged = Callback<LobbyChatUpdate_t>.Create(OnMembersChanged);
        _inviteAccepted = Callback<GameLobbyJoinRequested_t>.Create(OnInviteAccepted);
    }

    public bool IsOpen => _lobby.IsValid();

    public bool IsOwner => IsOpen && SteamMatchmaking.GetLobbyOwner(_lobby) == SteamUser.GetSteamID();

    /// <summary>The other player in the lobby, once there is one.</summary>
    public ulong? OpponentSteamId { get; private set; }

    public string OpponentName =>
        OpponentSteamId is { } opponent ? SteamFriends.GetFriendPersonaName(new CSteamID(opponent)) : string.Empty;

    /// <summary>Sends a Steam invite to this lobby. False until the lobby is open.</summary>
    public bool Invite(ulong friendSteamId) =>
        IsOpen && SteamMatchmaking.InviteUserToLobby(_lobby, new CSteamID(friendSteamId));

    public void Dispose()
    {
        if (IsOpen)
        {
            SteamMatchmaking.LeaveLobby(_lobby);
        }

        _created.Dispose();
        _entered.Dispose();
        _membersChanged.Dispose();
        _inviteAccepted.Dispose();
    }

    internal static SteamLobby Open()
    {
        var lobby = new SteamLobby();
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxMembers);
        return lobby;
    }

    private void OnCreated(LobbyCreated_t created)
    {
        // An invite was accepted while this lobby was still being created: the friend's lobby wins.
        if (created.m_eResult == EResult.k_EResultOK && _joiningFriendsLobby)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(created.m_ulSteamIDLobby));
        }
    }

    private void OnEntered(LobbyEnter_t entered)
    {
        _joiningFriendsLobby = false;
        _lobby = new CSteamID(entered.m_ulSteamIDLobby);
        FindOpponent();
    }

    private void OnMembersChanged(LobbyChatUpdate_t update)
    {
        if (update.m_ulSteamIDLobby == _lobby.m_SteamID)
        {
            FindOpponent();
        }
    }

    private void OnInviteAccepted(GameLobbyJoinRequested_t invite)
    {
        if (IsOpen)
        {
            SteamMatchmaking.LeaveLobby(_lobby);
        }

        _lobby = CSteamID.Nil;
        OpponentSteamId = null;
        _joiningFriendsLobby = true;
        SteamMatchmaking.JoinLobby(invite.m_steamIDLobby);
    }

    private void FindOpponent()
    {
        OpponentSteamId = null;
        CSteamID self = SteamUser.GetSteamID();
        int members = SteamMatchmaking.GetNumLobbyMembers(_lobby);
        for (int i = 0; i < members; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i);
            if (member != self)
            {
                OpponentSteamId = member.m_SteamID;
            }
        }
    }
}
