using Platform.Steam.Friends;
using Platform.Steam.Lobbies;
using Xunit;

namespace Platform.Steam.Tests.Lobbies;

public sealed class LobbyFlowTests
{
    private const ulong FriendLobby = 900;
    private static readonly SteamFriend _rival = new(42, "Rival");
    private static readonly SteamFriend _other = new(43, "Other");

    [Fact]
    public void StartsIdleOutsideAnyLobby()
    {
        using var flow = new LobbyFlow(new FakeLobby());

        flow.Update();

        Assert.Equal(LobbyStage.Idle, flow.Stage);
        Assert.False(flow.HasOpponent);
    }

    [Fact]
    public void HostingCreatesALobbyAndWaitsInItAsHost()
    {
        var lobby = new FakeLobby();
        using var flow = new LobbyFlow(lobby);

        flow.Host();
        Assert.Equal(LobbyStage.Entering, flow.Stage);
        Assert.Equal(1, lobby.Creates);

        lobby.Enter(asOwner: true);
        flow.Update();

        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.True(flow.IsHost);
        Assert.False(flow.HasOpponent);
    }

    [Fact]
    public void ReadyNeedsAnOpponent()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostAlone(lobby);

        flow.Ready();

        Assert.False(flow.IsLocalReady);
        Assert.Empty(lobby.ReadyMarks);
    }

    [Fact]
    public void ReadyMarksTheFirstMatch()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostWithRival(lobby);

        flow.Ready();

        Assert.True(flow.IsLocalReady);
        Assert.Equal([1], lobby.ReadyMarks);
        Assert.Equal(LobbyStage.InLobby, flow.Stage);
    }

    [Fact]
    public void OneReadyPlayerDoesNotStartTheMatch()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostWithRival(lobby);

        lobby.OpponentReadyFor = 1;
        flow.Update();

        Assert.True(flow.IsOpponentReady);
        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.Null(flow.TakeMatchToStart());
    }

    [Fact]
    public void BothReadyGoesToPicking()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostWithRival(lobby);

        flow.Ready();
        lobby.OpponentReadyFor = 1;
        flow.Update();

        Assert.Equal(LobbyStage.Picking, flow.Stage);
        Assert.Null(flow.TakeMatchToStart());
    }

    [Fact]
    public void BothPicksStartTheMatchOnceWithTheHostAsPlayer1()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = Picking(lobby);

        flow.Pick("mine");
        Assert.Equal(["1:mine"], lobby.PickMarks);
        Assert.Equal(LobbyStage.Picking, flow.Stage);
        lobby.OpponentPick = "1:theirs";
        flow.Update();

        Assert.Equal(LobbyStage.Starting, flow.Stage);
        Assert.Equal(
            new LobbyMatch(_rival.SteamId, "Rival", LocalIsPlayer1: true, Number: 1)
            {
                LocalPick = "mine",
                OpponentPick = "theirs",
            },
            flow.TakeMatchToStart());
        Assert.Equal(LobbyStage.InMatch, flow.Stage);
        Assert.Null(flow.TakeMatchToStart());
    }

    [Fact]
    public void TheOpponentsPickArrivingFirstWaitsForOurs()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = Picking(lobby);

        lobby.OpponentPick = "1:theirs";
        flow.Update();
        Assert.Equal("theirs", flow.OpponentPick);
        Assert.Equal(LobbyStage.Picking, flow.Stage);

        flow.Pick("mine");

        Assert.Equal(LobbyStage.Starting, flow.Stage);
    }

    [Fact]
    public void APickForAnotherMatchDoesNotCount()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = Picking(lobby);
        flow.Pick("mine");

        lobby.OpponentPick = "2:theirs";
        flow.Update();

        Assert.Empty(flow.OpponentPick);
        Assert.Equal(LobbyStage.Picking, flow.Stage);
    }

    [Fact]
    public void APickIsOnlyTakenWhilePickingAndOnlyOnce()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostWithRival(lobby);

        flow.Pick("early");
        Assert.Empty(lobby.PickMarks);

        flow.Ready();
        lobby.OpponentReadyFor = 1;
        flow.Update();
        flow.Pick("mine");
        flow.Pick("changed");

        Assert.Equal(["1:mine"], lobby.PickMarks);
        Assert.Equal("mine", flow.LocalPick);
    }

    [Fact]
    public void TheGuestPlaysPlayer2()
    {
        var lobby = new FakeLobby();
        using var flow = new LobbyFlow(lobby);
        lobby.Enter(asOwner: false);
        lobby.Opponent = _rival;
        flow.Update();

        flow.Ready();
        lobby.OpponentReadyFor = 1;
        flow.Update();
        flow.Pick("mine");
        lobby.OpponentPick = "1:theirs";
        flow.Update();

        Assert.False(flow.IsHost);
        Assert.False(flow.TakeMatchToStart()!.LocalIsPlayer1);
    }

    [Fact]
    public void AFinishedMatchOffersARematchThatTheOldReadyMarksCantStart()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);

        flow.MatchEnded();
        flow.Update();

        Assert.Equal(LobbyStage.Rematch, flow.Stage);
        Assert.Equal(2, flow.MatchNumber);
        Assert.False(flow.IsLocalReady);
        Assert.False(flow.IsOpponentReady);

        flow.Ready();
        Assert.Equal(LobbyStage.Rematch, flow.Stage);
        Assert.Equal([1, 2], lobby.ReadyMarks);
    }

    [Fact]
    public void BothWantingARematchStartsTheNextMatchWithTheSamePicks()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);
        flow.MatchEnded();

        flow.Ready();
        Assert.Equal("2:mine", lobby.LastPickMark);
        lobby.OpponentReadyFor = 2;
        lobby.OpponentPick = "2:theirs";
        flow.Update();

        Assert.Equal(LobbyStage.Starting, flow.Stage);
        LobbyMatch rematch = flow.TakeMatchToStart()!;
        Assert.Equal(2, rematch.Number);
        Assert.Equal("mine", rematch.LocalPick);
        Assert.Equal("theirs", rematch.OpponentPick);
    }

    [Fact]
    public void ARematchWaitsForTheOpponentsPickForIt()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);
        flow.MatchEnded();

        flow.Ready();
        lobby.OpponentReadyFor = 2;
        flow.Update();

        Assert.Equal(LobbyStage.Rematch, flow.Stage);
    }

    [Fact]
    public void TheOpponentsRematchMarkCountsEvenWhenItArrivesBeforeOurMatchEnds()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);

        lobby.OpponentReadyFor = 2;
        lobby.OpponentPick = "2:theirs";
        flow.Update();
        Assert.Equal(LobbyStage.InMatch, flow.Stage);

        flow.MatchEnded();
        Assert.True(flow.IsOpponentReady);
        flow.Ready();

        Assert.Equal(LobbyStage.Starting, flow.Stage);
    }

    [Fact]
    public void TheOpponentLeavingMidMatchWaitsUntilTheMatchEnds()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);

        lobby.Opponent = null;
        flow.Update();
        Assert.Equal(LobbyStage.InMatch, flow.Stage);

        flow.MatchEnded();

        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.False(flow.HasOpponent);
        Assert.Equal(new LobbyNotice(LobbyNoticeKind.OpponentLeft, "Rival"), flow.Notice);
    }

    [Fact]
    public void ANewOpponentStartsAgainAtMatch1WithNobodyReady()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);
        flow.MatchEnded();
        flow.Ready();

        lobby.Opponent = _other;
        lobby.OpponentReadyFor = 0;
        flow.Update();

        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.Equal("Other", flow.OpponentName);
        Assert.Equal(1, flow.MatchNumber);
        Assert.False(flow.IsLocalReady);
        Assert.Equal(0, lobby.LastReadyMark);
        Assert.Empty(flow.LocalPick);
        Assert.Empty(lobby.LastPickMark);
    }

    [Fact]
    public void TheOpponentLeavingBeforeTheGameTakesTheMatchCancelsIt()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostWithRival(lobby);
        flow.Ready();
        lobby.OpponentReadyFor = 1;
        flow.Update();

        lobby.Opponent = null;
        flow.Update();

        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.Null(flow.TakeMatchToStart());
    }

    [Fact]
    public void ADisconnectedOpponentEndsTheMatchAndLeavesTheLobbyWithoutARematch()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);

        flow.OpponentDisconnected();
        flow.Update();

        Assert.Equal(LobbyStage.Idle, flow.Stage);
        Assert.Equal(1, lobby.Leaves);
        Assert.False(flow.HasOpponent);
        Assert.Equal(new LobbyNotice(LobbyNoticeKind.ConnectionLost, "Rival"), flow.Notice);
    }

    [Fact]
    public void ADisconnectAfterTheMatchCancelsTheRematch()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);
        flow.MatchEnded();

        flow.OpponentDisconnected();

        Assert.Equal(LobbyStage.Idle, flow.Stage);
        Assert.Equal(1, lobby.Leaves);
    }

    [Fact]
    public void ADisconnectBeforeAnyMatchChangesNothing()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostWithRival(lobby);

        flow.OpponentDisconnected();

        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.Equal(0, lobby.Leaves);
        Assert.True(flow.Notice.IsNone);
    }

    [Fact]
    public void LeavingFromAMatchReturnsToIdle()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = InMatch(lobby);

        flow.Leave();
        flow.Update();

        Assert.Equal(LobbyStage.Idle, flow.Stage);
        Assert.Equal(1, lobby.Leaves);
        Assert.False(flow.HasOpponent);
    }

    [Fact]
    public void LosingTheLobbyReturnsToIdle()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostWithRival(lobby);

        lobby.Presence = LobbyPresence.Outside;
        flow.Update();

        Assert.Equal(LobbyStage.Idle, flow.Stage);
    }

    [Fact]
    public void AFailedCreateSaysWhyAndReturnsToIdle()
    {
        var lobby = new FakeLobby();
        using var flow = new LobbyFlow(lobby);
        flow.Host();

        lobby.Presence = LobbyPresence.Outside;
        lobby.Failure = new LobbyNotice(LobbyNoticeKind.CreateFailed, "k_EResultTimeout");
        flow.Update();

        Assert.Equal(LobbyStage.Idle, flow.Stage);
        Assert.Equal(new LobbyNotice(LobbyNoticeKind.CreateFailed, "k_EResultTimeout"), flow.Notice);
    }

    [Fact]
    public void AnAcceptedInviteLeavesOurLobbyForTheFriends()
    {
        var lobby = new FakeLobby();
        using LobbyFlow flow = HostAlone(lobby);

        lobby.AcceptedInvite = FriendLobby;
        flow.Update();

        Assert.Equal(1, lobby.Leaves);
        Assert.Equal([FriendLobby], lobby.Joined);
        Assert.Equal(LobbyStage.Entering, flow.Stage);

        lobby.Enter(asOwner: false);
        lobby.Opponent = _rival;
        flow.Update();

        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.False(flow.IsHost);
        Assert.Equal("Rival", flow.OpponentName);
    }

    [Fact]
    public void AnInviteAcceptedFromIdleJoinsWithoutLeaving()
    {
        var lobby = new FakeLobby();
        using var flow = new LobbyFlow(lobby);

        lobby.AcceptedInvite = FriendLobby;
        flow.Update();

        Assert.Equal(0, lobby.Leaves);
        Assert.Equal([FriendLobby], lobby.Joined);
    }

    [Fact]
    public void InvitesGoToTheListedFriendWhileWaitingForAnOpponent()
    {
        var lobby = new FakeLobby();
        lobby.Friends.Add(_rival);
        lobby.Friends.Add(_other);
        using LobbyFlow flow = HostAlone(lobby);
        flow.RefreshFriends();

        Assert.True(flow.Invite(1));
        Assert.False(flow.Invite(2));
        Assert.Equal([_other.SteamId], lobby.Invited);
    }

    [Fact]
    public void JoiningAFriendEntersTheirLobby()
    {
        var lobby = new FakeLobby();
        lobby.Friends.Add(_rival);
        lobby.Friends.Add(_other with { LobbyId = FriendLobby });
        using var flow = new LobbyFlow(lobby);
        flow.RefreshFriends();

        flow.JoinFriend(0);
        Assert.Equal(LobbyStage.Idle, flow.Stage);

        flow.JoinFriend(1);
        Assert.Equal(LobbyStage.Entering, flow.Stage);
        Assert.Equal([FriendLobby], lobby.Joined);

        lobby.Enter(asOwner: false);
        lobby.Opponent = _other;
        flow.Update();

        Assert.Equal(LobbyStage.InLobby, flow.Stage);
        Assert.False(flow.IsHost);
    }

    [Fact]
    public void JoiningAFriendNeedsToBeOutOfALobby()
    {
        var lobby = new FakeLobby();
        lobby.Friends.Add(_rival with { LobbyId = FriendLobby });
        using LobbyFlow flow = HostAlone(lobby);
        flow.RefreshFriends();

        flow.JoinFriend(0);

        Assert.Empty(lobby.Joined);
        Assert.Equal(0, lobby.Leaves);
    }

    [Fact]
    public void InvitesNeedALobby()
    {
        var lobby = new FakeLobby();
        lobby.Friends.Add(_rival);
        using var flow = new LobbyFlow(lobby);
        flow.RefreshFriends();

        Assert.False(flow.Invite(0));
        Assert.False(flow.OpenInviteOverlay());
        Assert.Empty(lobby.Invited);
    }

    [Fact]
    public void DisposingDisposesTheSteamLobby()
    {
        var lobby = new FakeLobby();
        var flow = new LobbyFlow(lobby);

        flow.Dispose();

        Assert.True(lobby.Disposed);
    }

    // Both PCs, each seeing the other's last ready mark: the first match and a rematch start on both, same number.
    [Fact]
    public void TwoPcsStartTheSameMatchesWithOppositePorts()
    {
        var hostLobby = new FakeLobby();
        var guestLobby = new FakeLobby();
        using LobbyFlow host = HostAlone(hostLobby);
        using var guest = new LobbyFlow(guestLobby);
        guestLobby.Enter(asOwner: false);
        hostLobby.Opponent = new SteamFriend(2, "Guest");
        guestLobby.Opponent = new SteamFriend(1, "Host");

        for (int expectedNumber = 1; expectedNumber <= 2; expectedNumber++)
        {
            Exchange(hostLobby, guestLobby, host, guest);
            host.Ready();
            Exchange(hostLobby, guestLobby, host, guest);
            Assert.Null(guest.TakeMatchToStart());
            guest.Ready();
            Exchange(hostLobby, guestLobby, host, guest);
            if (expectedNumber == 1)
            {
                Assert.Equal(LobbyStage.Picking, host.Stage);
                host.Pick("host pick");
                guest.Pick("guest pick");
                Exchange(hostLobby, guestLobby, host, guest);
            }

            LobbyMatch? hostMatch = host.TakeMatchToStart();
            LobbyMatch? guestMatch = guest.TakeMatchToStart();
            Assert.Equal(
                new LobbyMatch(2, "Guest", LocalIsPlayer1: true, expectedNumber)
                {
                    LocalPick = "host pick",
                    OpponentPick = "guest pick",
                },
                hostMatch);
            Assert.Equal(
                new LobbyMatch(1, "Host", LocalIsPlayer1: false, expectedNumber)
                {
                    LocalPick = "guest pick",
                    OpponentPick = "host pick",
                },
                guestMatch);

            host.MatchEnded();
            guest.MatchEnded();
        }
    }

    private static void Exchange(FakeLobby hostLobby, FakeLobby guestLobby, LobbyFlow host, LobbyFlow guest)
    {
        hostLobby.OpponentReadyFor = guestLobby.LastReadyMark;
        guestLobby.OpponentReadyFor = hostLobby.LastReadyMark;
        hostLobby.OpponentPick = guestLobby.LastPickMark;
        guestLobby.OpponentPick = hostLobby.LastPickMark;
        host.Update();
        guest.Update();
    }

    private static LobbyFlow HostAlone(FakeLobby lobby)
    {
        var flow = new LobbyFlow(lobby);
        flow.Host();
        lobby.Enter(asOwner: true);
        flow.Update();
        return flow;
    }

    private static LobbyFlow HostWithRival(FakeLobby lobby)
    {
        LobbyFlow flow = HostAlone(lobby);
        lobby.Opponent = _rival;
        flow.Update();
        return flow;
    }

    private static LobbyFlow Picking(FakeLobby lobby)
    {
        LobbyFlow flow = HostWithRival(lobby);
        flow.Ready();
        lobby.OpponentReadyFor = 1;
        flow.Update();
        return flow;
    }

    private static LobbyFlow InMatch(FakeLobby lobby)
    {
        LobbyFlow flow = Picking(lobby);
        flow.Pick("mine");
        lobby.OpponentPick = "1:theirs";
        flow.Update();
        Assert.NotNull(flow.TakeMatchToStart());
        return flow;
    }
}
