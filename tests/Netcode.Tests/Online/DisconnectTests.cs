using System;
using System.Diagnostics;
using Netcode.Online;
using Netcode.Tests.Transport;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Online;

/// <summary>
/// A pulled cable between two real Testman peers (ADR-0005 §4): a warning after the notify time, the end without a
/// result after the disconnect time, on both PCs; a connection back in time resumes the match.
/// </summary>
public sealed class DisconnectTests
{
    private const int FightingFrames = 2000;
    private const int CutFrame = 60;
    private const int FramesAfterRecovery = 200;

    // The rollback window and input delay let each PC run a few frames past the last input it got, never more.
    private const int MostFramesPastTheCut = 40;

    // Short stand-ins for data/netcode/online.json's 3 s and 10 s, so the tests stay quick.
    private static readonly DisconnectTimeouts _timeouts = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2));

    // The last packet before the cut may have arrived a little earlier than the cut itself.
    private static readonly TimeSpan _slack = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void ASilentConnectionWarnsAfterTheNotifyTime()
    {
        using var match = new InMemoryMatch(FightingFrames, disconnect: _timeouts);
        match.RunUntilFrame(CutFrame);

        CutBothWays(match);
        var clock = Stopwatch.StartNew();
        match.RunUntil(() => State(match.Player1Connection) == ConnectionState.Interrupted);

        Assert.InRange(clock.Elapsed, _timeouts.WarningAfter - _slack, _timeouts.EndMatchAfter);
        ConnectionStatus status = match.Player1Connection.ReadConnectionStatus();
        Assert.InRange(status.SecondsUntilDisconnect, 1, SecondsBetweenWarningAndEnd());
    }

    [Fact]
    public void BothPeersEndTheMatchWithoutAResultAfterTheDisconnectTime()
    {
        using var match = new InMemoryMatch(FightingFrames, disconnect: _timeouts);
        match.RunUntilFrame(CutFrame);

        CutBothWays(match);
        var clock = Stopwatch.StartNew();
        match.RunUntil(() => State(match.Player1Connection) == ConnectionState.Disconnected &&
            State(match.Player2Connection) == ConnectionState.Disconnected);

        Assert.True(clock.Elapsed >= _timeouts.EndMatchAfter - _slack, $"Ended after {clock.Elapsed}.");
        AssertHeldWithoutAResult(match);
    }

    [Fact]
    public void ALinkCutOneWayStillEndsTheMatchOnBothPeers()
    {
        using var match = new InMemoryMatch(FightingFrames, disconnect: _timeouts);
        match.RunUntilFrame(CutFrame);

        match.Player1Transport.Cut();
        match.RunUntil(() => State(match.Player1Connection) == ConnectionState.Disconnected &&
            State(match.Player2Connection) == ConnectionState.Disconnected);

        AssertHeldWithoutAResult(match);
    }

    [Fact]
    public void AConnectionBackBeforeTheEndResumesTheMatch()
    {
        using var match = new InMemoryMatch(FightingFrames, disconnect: _timeouts);
        match.RunUntilFrame(CutFrame);

        CutBothWays(match);
        match.RunUntil(() => State(match.Player1Connection) == ConnectionState.Interrupted &&
            State(match.Player2Connection) == ConnectionState.Interrupted);
        match.Player1Transport.Restore();
        match.Player2Transport.Restore();
        match.RunUntilFrame(CutFrame + FramesAfterRecovery);

        Assert.Equal(ConnectionState.Connected, State(match.Player1Connection));
        Assert.Equal(ConnectionState.Connected, State(match.Player2Connection));
        Assert.Equal(StateChecksum.Compute(in match.Player1.State), StateChecksum.Compute(in match.Player2.State));
    }

    private static void CutBothWays(InMemoryMatch match)
    {
        match.Player1Transport.Cut();
        match.Player2Transport.Cut();
    }

    // Neither PC plays on alone or declares a winner: the match stops where the inputs ran out.
    private static void AssertHeldWithoutAResult(InMemoryMatch match)
    {
        int player1Frame = match.Player1.CurrentFrame;
        int player2Frame = match.Player2.CurrentFrame;
        Assert.InRange(player1Frame, CutFrame, CutFrame + MostFramesPastTheCut);
        Assert.InRange(player2Frame, CutFrame, CutFrame + MostFramesPastTheCut);
        Assert.NotEqual(RoundPhase.MatchOver, match.Player1.State.Round.Phase);
        Assert.NotEqual(RoundPhase.MatchOver, match.Player2.State.Round.Phase);

        var clock = Stopwatch.StartNew();
        match.RunUntil(() => clock.Elapsed > _timeouts.WarningAfter);

        Assert.Equal((player1Frame, player2Frame), (match.Player1.CurrentFrame, match.Player2.CurrentFrame));
    }

    private static ConnectionState State(IConnectionStatusReadout connection) => connection.ReadConnectionStatus().State;

    private static int SecondsBetweenWarningAndEnd() =>
        (int)Math.Ceiling((_timeouts.EndMatchAfter - _timeouts.WarningAfter).TotalSeconds);
}
