using System;
using System.Collections.Generic;
using System.Globalization;
using Platform.Steam.Friends;

namespace Platform.Steam.Lobbies;

/// <summary>
/// The lobby from this player's side: host one, join a friend's or accept their invite, both ready → both pick → match,
/// then rematch with the same picks (<see cref="LobbyStage"/>, ADR-0042). Steam sits behind <see cref="ILobby"/>; call <see cref="Update"/> once per displayed
/// frame, after Steam's callbacks.
/// </summary>
/// <remarks>
/// - Each PC decides on its own that both are ready and both picked; there is no start message. Ready and pick marks
///   name the match they are for (1, 2, …), so a mark left over from the last match can never start the next one, and
///   the PCs never need to agree on when a match ended.
/// - Ready and picks can't be taken back: one PC may already have seen both marks and started, and the other would
///   never follow. Leaving the lobby is the way out.
/// - Picks are opaque text to the lobby; the game writes and reads them.
/// - Match numbers count per opponent: when the opponent leaves or changes, the next match is number 1 again.
/// - While a match runs, lobby changes wait until it ends (<see cref="MatchEnded"/>); the netcode handles an opponent
///   who disappears mid-match. An accepted invite is the exception: the player chose to go.
/// </remarks>
public sealed class LobbyFlow : IDisposable
{
    private const int FirstMatch = 1;
    private const int NotReady = 0;
    private const char PickSeparator = ':';

    private readonly ILobby _lobby;
    private IReadOnlyList<SteamFriend> _friends = [];
    private ulong? _opponentSteamId;
    private string _opponentName = string.Empty;
    private int _matchNumber = FirstMatch;
    private string _localPick = string.Empty;

    public LobbyFlow(ILobby lobby)
    {
        _lobby = lobby;
    }

    public LobbyStage Stage { get; private set; } = LobbyStage.Idle;

    /// <summary>Whether this PC owns the lobby and plays P1.</summary>
    public bool IsHost => _lobby.Presence == LobbyPresence.Inside && _lobby.IsOwner;

    public bool HasOpponent => _opponentSteamId is not null;

    /// <summary>The opponent's Steam name; empty while there is none.</summary>
    public string OpponentName => _opponentName;

    /// <summary>The match being readied or played: 1 for the first against this opponent, 2 for the first rematch.</summary>
    public int MatchNumber => _matchNumber;

    public bool IsLocalReady { get; private set; }

    public bool IsOpponentReady => HasOpponent && _lobby.OpponentReadyFor == _matchNumber;

    /// <summary>This player's pick for the match being readied (kept for rematches); empty before they picked.</summary>
    public string LocalPick => _localPick;

    /// <summary>The opponent's pick for the match being readied; empty until it arrives.</summary>
    public string OpponentPick => HasOpponent ? PickFor(_lobby.OpponentPick, _matchNumber) : string.Empty;

    /// <summary>Round trip to the opponent as Steam estimates it before a match; null while unknown or alone.</summary>
    public int? OpponentPingMilliseconds => HasOpponent ? _lobby.OpponentPingMilliseconds : null;

    /// <summary>The last thing that went wrong or changed on its own (a failed join, the opponent leaving).</summary>
    public LobbyNotice Notice { get; private set; } = LobbyNotice.None;

    /// <summary>
    /// Friends running this game, as of the last <see cref="RefreshFriends"/>: the ones who can be invited, and those
    /// in a lobby can be joined (<see cref="JoinFriend"/>).
    /// </summary>
    public IReadOnlyList<SteamFriend> InvitableFriends => _friends;

    /// <summary>Creates a lobby to invite a friend to. Only from <see cref="LobbyStage.Idle"/>.</summary>
    public void Host()
    {
        if (Stage != LobbyStage.Idle)
        {
            return;
        }

        Notice = LobbyNotice.None;
        _lobby.Create();
        Stage = LobbyStage.Entering;
    }

    /// <summary>Joins the lobby of the friend at this index of <see cref="InvitableFriends"/>. Only from <see cref="LobbyStage.Idle"/>.</summary>
    public void JoinFriend(int friendIndex)
    {
        if (Stage != LobbyStage.Idle || friendIndex < 0 || friendIndex >= _friends.Count || !_friends[friendIndex].HasLobby)
        {
            return;
        }

        JoinInvited(_friends[friendIndex].LobbyId);
    }

    /// <summary>
    /// Marks this player ready for the next match: in the lobby, on to picking; for a rematch, with the same pick as
    /// the last match. Needs an opponent.
    /// </summary>
    public void Ready()
    {
        if (Stage is not (LobbyStage.InLobby or LobbyStage.Rematch) || !HasOpponent || IsLocalReady)
        {
            return;
        }

        if (Stage == LobbyStage.Rematch)
        {
            MarkPick();
        }

        IsLocalReady = true;
        _lobby.MarkReadyFor(_matchNumber);
        Settle();
    }

    /// <summary>This player's pick for the match, once, while <see cref="LobbyStage.Picking"/>.</summary>
    /// <param name="pick">Opaque to the lobby; must not be empty.</param>
    public void Pick(string pick)
    {
        ArgumentException.ThrowIfNullOrEmpty(pick);
        if (Stage != LobbyStage.Picking || _localPick.Length > 0)
        {
            return;
        }

        _localPick = pick;
        MarkPick();
        Settle();
    }

    /// <summary>Leaves the lobby from any stage, also ending a running match.</summary>
    public void Leave()
    {
        if (Stage == LobbyStage.Idle)
        {
            return;
        }

        _lobby.Leave();
        Forget();
        Stage = LobbyStage.Idle;
    }

    /// <summary>Invites the friend at this index of <see cref="InvitableFriends"/>. False when that didn't work.</summary>
    public bool Invite(int friendIndex) =>
        Stage == LobbyStage.InLobby && friendIndex >= 0 && friendIndex < _friends.Count &&
        _lobby.Invite(_friends[friendIndex].SteamId);

    /// <summary>Opens Steam's overlay to invite any friend. False when there is no lobby or no overlay.</summary>
    public bool OpenInviteOverlay() => Stage == LobbyStage.InLobby && _lobby.OpenInviteOverlay();

    public void RefreshFriends() => _friends = _lobby.FriendsPlayingThisGame();

    /// <summary>Catches up with what Steam reported: entering, invites, the opponent coming and going, ready marks.</summary>
    public void Update()
    {
        LobbyNotice failure = _lobby.TakeFailure();
        if (!failure.IsNone)
        {
            Notice = failure;
        }

        if (_lobby.TakeAcceptedInvite() is { } invitedLobby)
        {
            JoinInvited(invitedLobby);
        }

        Settle();
    }

    /// <summary>The match to start now that both players are ready; handed over once per match, null otherwise.</summary>
    public LobbyMatch? TakeMatchToStart()
    {
        if (Stage != LobbyStage.Starting || _opponentSteamId is not { } opponent)
        {
            return null;
        }

        Stage = LobbyStage.InMatch;
        return new LobbyMatch(opponent, _opponentName, LocalIsPlayer1: _lobby.IsOwner, _matchNumber)
        {
            LocalPick = _localPick,
            OpponentPick = OpponentPick,
        };
    }

    /// <summary>The game says the match is over (or never started): the lobby offers a rematch.</summary>
    public void MatchEnded()
    {
        if (Stage != LobbyStage.InMatch)
        {
            return;
        }

        Stage = LobbyStage.Rematch;
        _matchNumber++;
        IsLocalReady = false;
        Settle();
    }

    /// <summary>
    /// The game says the opponent's PC stopped answering during the match or after it: no result, and no rematch with
    /// an opponent who is gone, so the lobby is left with a notice saying why.
    /// </summary>
    public void OpponentDisconnected()
    {
        if (Stage is not (LobbyStage.InMatch or LobbyStage.Rematch))
        {
            return;
        }

        string opponent = _opponentName;
        Leave();
        Notice = new LobbyNotice(LobbyNoticeKind.ConnectionLost, opponent);
    }

    public void Dispose() => _lobby.Dispose();

    private void JoinInvited(ulong lobbyId)
    {
        if (Stage != LobbyStage.Idle)
        {
            _lobby.Leave();
        }

        Forget();
        Notice = LobbyNotice.None;
        _lobby.Join(lobbyId);
        Stage = LobbyStage.Entering;
    }

    private void Settle()
    {
        if (Stage is LobbyStage.InMatch)
        {
            return;
        }

        switch (_lobby.Presence)
        {
            case LobbyPresence.Outside:
                Forget();
                Stage = LobbyStage.Idle;
                return;
            case LobbyPresence.Entering:
                Stage = LobbyStage.Entering;
                return;
        }

        if (Stage is LobbyStage.Idle or LobbyStage.Entering)
        {
            Stage = LobbyStage.InLobby;
        }

        FollowOpponent();
        bool bothReady = IsLocalReady && IsOpponentReady;
        if (Stage == LobbyStage.InLobby && bothReady)
        {
            Stage = LobbyStage.Picking;
        }

        if (Stage is LobbyStage.Picking or LobbyStage.Rematch && bothReady && _localPick.Length > 0 &&
            OpponentPick.Length > 0)
        {
            Stage = LobbyStage.Starting;
        }
    }

    // The pick in a mark when the mark is for this match; empty otherwise.
    private static string PickFor(string mark, int matchNumber)
    {
        int separator = mark.IndexOf(PickSeparator, StringComparison.Ordinal);
        return separator > 0 &&
            int.TryParse(mark.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out int number) &&
            number == matchNumber
            ? mark[(separator + 1)..]
            : string.Empty;
    }

    private void MarkPick() =>
        _lobby.MarkPick(string.Create(CultureInfo.InvariantCulture, $"{_matchNumber}{PickSeparator}{_localPick}"));

    // A different opponent (or none) is a new pairing: back to the lobby, match 1, nobody ready.
    private void FollowOpponent()
    {
        SteamFriend? opponent = _lobby.Opponent;
        if (opponent?.SteamId == _opponentSteamId)
        {
            return;
        }

        if (opponent is null && _opponentName.Length > 0)
        {
            Notice = new LobbyNotice(LobbyNoticeKind.OpponentLeft, _opponentName);
        }

        _opponentSteamId = opponent?.SteamId;
        _opponentName = opponent?.Name ?? string.Empty;
        _matchNumber = FirstMatch;
        if (IsLocalReady)
        {
            IsLocalReady = false;
            _lobby.MarkReadyFor(NotReady);
        }

        if (_localPick.Length > 0)
        {
            _localPick = string.Empty;
            _lobby.MarkPick(string.Empty);
        }

        Stage = LobbyStage.InLobby;
    }

    // Out of the lobby: nothing about the last one carries over.
    private void Forget()
    {
        _opponentSteamId = null;
        _opponentName = string.Empty;
        _matchNumber = FirstMatch;
        _localPick = string.Empty;
        IsLocalReady = false;
    }
}
