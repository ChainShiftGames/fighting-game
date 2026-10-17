using System;
using FightingGame.UI;
using Netcode.Online;
using Xunit;

namespace FightingGame.Tests.UI;

public sealed class MatchStatusTextTests
{
    private static readonly ConnectionStatus _interrupted = new(ConnectionState.Interrupted, 7);

    [Fact]
    public void ARunningMatchShowsNothing() =>
        Assert.Empty(MatchStatusText.Choose(string.Empty, ConnectionStatus.Connected, string.Empty, string.Empty));

    [Fact]
    public void AnInterruptionCountsDownTheSecondsLeft()
    {
        string text = MatchStatusText.Choose(string.Empty, _interrupted, string.Empty, string.Empty);

        Assert.StartsWith("Connection interrupted", text, StringComparison.Ordinal);
        Assert.Contains("ends in 7 s", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ADisconnectSaysTheMatchEndedWithoutAResult() => Assert.Equal(
        MatchStatusText.OpponentDisconnected,
        MatchStatusText.Choose(string.Empty, ConnectionStatus.Disconnected, string.Empty, string.Empty));

    [Fact]
    public void ADesyncComesBeforeEverythingElse()
    {
        string text = MatchStatusText.Choose("Checksums differ at frame 9.", _interrupted, "Waiting…", "PAUSED");

        Assert.Equal("DESYNC — match stopped\nChecksums differ at frame 9.", text);
    }

    [Fact]
    public void TheConnectionComesBeforeWaiting() => Assert.StartsWith(
        "Connection interrupted",
        MatchStatusText.Choose(string.Empty, _interrupted, "Waiting…", string.Empty),
        StringComparison.Ordinal);

    [Fact]
    public void WaitingComesBeforePaused() =>
        Assert.Equal("Waiting…", MatchStatusText.Choose(string.Empty, ConnectionStatus.Connected, "Waiting…", "PAUSED"));

    [Fact]
    public void PausedShowsWhenNothingElseDoes() =>
        Assert.Equal("PAUSED", MatchStatusText.Choose(string.Empty, ConnectionStatus.Connected, string.Empty, "PAUSED"));
}
