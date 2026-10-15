using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Tests.Drivers;
using Netcode.Transport;
using Sim.Input;
using Sim.State;
using Sim.Toy;
using Xunit;

namespace Netcode.Tests.Online;

/// <summary>Two peers in one process over loopback UDP, with the ADR-0005 bad-network test conditions.</summary>
public sealed class PeerSessionTests
{
    private const int Frames = 900;
    private const int SettleFrames = 60;
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public void BothPeersEndInTheSameStateOverABadNetwork()
    {
        using var player1 = CreateDriver(new ToyMatchSimulation(new ToySimulation(ToyRules.Default)), 47011, 47012, true);
        using var player2 = CreateDriver(new ToyMatchSimulation(new ToySimulation(ToyRules.Default)), 47012, 47011, false);

        RunUntil(player1, player2, Frames + SettleFrames);

        // Inputs go neutral after Frames, so by the end every frame both states depend on is confirmed.
        Assert.Equal(StateChecksum.Compute(in player1.State), StateChecksum.Compute(in player2.State));
    }

    [Fact]
    public void ConnectionStatsShowThePingAndTheRollbacksOfABadNetwork()
    {
        using var player1 = CreateDriver(new ToyMatchSimulation(new ToySimulation(ToyRules.Default)), 47031, 47032, true);
        using var player2 = CreateDriver(new ToyMatchSimulation(new ToySimulation(ToyRules.Default)), 47032, 47031, false);

        RunUntil(player1, player2, Frames);

        Assert.True(player1.TryReadConnection(out ConnectionStats stats));
        int simulatedPing = NetworkSimulation.TestConditions.PingMilliseconds;
        Assert.InRange(stats.PingMilliseconds, simulatedPing / 2, simulatedPing * 3);
        Assert.True(stats.Rollbacks > 0, "Random inputs at 100 ms should mispredict.");
        Assert.InRange(stats.LongestRollbackFrames, 1, Settings(47031, 47032, true).RollbackFrames);
        Assert.True(stats.RolledBackFrames >= stats.Rollbacks);
    }

    [Fact]
    public void ADesyncBetweenPeersIsCaught()
    {
        using var player1 = CreateDriver(new ToyMatchSimulation(new ToySimulation(ToyRules.Default)), 47021, 47022, true);
        using var player2 = CreateDriver(new NonDeterministicSimulation(), 47022, 47021, false);

        Assert.Throws<DesyncDetectedException>(() => RunUntil(player1, player2, int.MaxValue));
    }

    private static RollbackMatchDriver<ToyState> CreateDriver(
        IMatchSimulation<ToyState> simulation, int localPort, int remotePort, bool localIsPlayer1) =>
        new(simulation, PeerSession.Build<ToyState>(Settings(localPort, remotePort, localIsPlayer1), PeerTransport.DirectUdp));

    private static PeerMatchSettings Settings(int localPort, int remotePort, bool localIsPlayer1) => new()
    {
        LocalPort = localPort,
        RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, remotePort),
        LocalIsPlayer1 = localIsPlayer1,
        NetworkSimulation = NetworkSimulation.TestConditions,
    };

    private static void RunUntil(RollbackMatchDriver<ToyState> player1, RollbackMatchDriver<ToyState> player2, int frames)
    {
        var inputs1 = new SeededInputs(1);
        var inputs2 = new SeededInputs(2);
        var clock = Stopwatch.StartNew();
        while (player1.CurrentFrame < frames || player2.CurrentFrame < frames)
        {
            Assert.True(clock.Elapsed < _timeout, $"Stuck at frames {player1.CurrentFrame} / {player2.CurrentFrame}.");
            Advance(player1, inputs1, frames);
            Advance(player2, inputs2, frames);

            // Roughly the game's pace, so the session sees time pass between frames.
            Thread.Sleep(1);
        }
    }

    private static void Advance(RollbackMatchDriver<ToyState> driver, SeededInputs inputs, int frames)
    {
        if (driver.CurrentFrame >= frames)
        {
            return;
        }

        InputWord input = driver.CurrentFrame < Frames ? inputs.Next() : InputWord.Neutral;
        driver.TryAdvanceFrame([input]);
    }
}
