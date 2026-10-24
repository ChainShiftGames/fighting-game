using FightingGame.Online;
using FightingGame.UI.Results;
using Netcode.Handshake;
using Xunit;

namespace FightingGame.Tests.UI.Results;

public sealed class ResultsEntriesTests
{
    [Fact]
    public void VersusOffersRematchCharacterSelectAndTheMainMenu()
    {
        Assert.Equal(
            [ResultsEntry.Rematch, ResultsEntry.CharacterSelect, ResultsEntry.MainMenu],
            ResultsEntries.For(LaunchMode.Versus, canRematch: true, canReselect: true, isOnline: false, HandshakeRefusal.None));
    }

    [Fact]
    public void AReplayOffersToWatchItAgain()
    {
        Assert.Equal(
            [ResultsEntry.WatchAgain, ResultsEntry.MainMenu],
            ResultsEntries.For(LaunchMode.Replay, canRematch: true, canReselect: false, isOnline: false, HandshakeRefusal.None));
    }

    [Fact]
    public void OnlineOffersRematchAndLeavingTheLobby()
    {
        Assert.Equal(
            [ResultsEntry.Rematch, ResultsEntry.LeaveLobby, ResultsEntry.MainMenu],
            ResultsEntries.For(LaunchMode.Online, canRematch: true, canReselect: false, isOnline: true, HandshakeRefusal.None));
    }

    [Fact]
    public void ARefusedOnlineMatchHasNoRematch()
    {
        Assert.Equal(
            [ResultsEntry.LeaveLobby, ResultsEntry.MainMenu],
            ResultsEntries.For(
                LaunchMode.Online, canRematch: true, canReselect: false, isOnline: true, HandshakeRefusal.GameVersion));
    }

    [Fact]
    public void AMatchThatCantBePlayedAgainOnlyGoesBack()
    {
        Assert.Equal(
            [ResultsEntry.MainMenu],
            ResultsEntries.For(
                LaunchMode.DirectOnline, canRematch: false, canReselect: false, isOnline: false, HandshakeRefusal.None));
    }
}
