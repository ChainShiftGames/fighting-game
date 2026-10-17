using Netcode.Transport;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>
/// P2-11: turning the network simulator mid-match changes only the packets, never the outcome (ARCHITECTURE.md §6).
/// </summary>
public sealed class LiveNetworkMatchTests
{
    private const int FightingFrames = 600;
    private const int SettleFrames = 60;
    private const int BadNetworkFrom = 100;
    private const int WorseNetworkFrom = 300;
    private const int GoodNetworkFrom = 450;

    private static readonly NetworkSimulation _worse =
        new() { PingMilliseconds = 200, JitterMilliseconds = 50, PacketLossPercent = 10 };

    [Fact]
    public void BothPeersEndInTheSameStateWhileTheConditionsChange()
    {
        var live = new LiveNetworkSimulation();
        using var match = new InMemoryMatch(FightingFrames, network: NetworkSimulation.Following(live));

        match.RunUntilFrame(FightingFrames + SettleFrames, () => live.Current = match.Player1.CurrentFrame switch
        {
            >= GoodNetworkFrom => NetworkSimulation.None,
            >= WorseNetworkFrom => _worse,
            >= BadNetworkFrom => NetworkSimulation.TestConditions,
            _ => NetworkSimulation.None,
        });

        // Inputs go neutral after FightingFrames, so by the end every frame both states depend on is confirmed.
        Assert.Equal(StateChecksum.Compute(in match.Player1.State), StateChecksum.Compute(in match.Player2.State));
    }
}
