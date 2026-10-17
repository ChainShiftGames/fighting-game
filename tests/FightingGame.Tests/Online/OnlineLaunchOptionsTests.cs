using System;
using FightingGame.Online;
using Netcode.Transport;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class OnlineLaunchOptionsTests
{
    [Fact]
    public void NoOptionsMeanNoSimulation() => Assert.Equal(NetworkSimulation.None, OnlineLaunchOptions.Simulation([]));

    [Fact]
    public void SingleOptionsChangeTheTestConditions()
    {
        NetworkSimulation simulation = OnlineLaunchOptions.Simulation(["--lag", "--loss", "5"]);

        Assert.Equal(NetworkSimulation.TestConditions with { PacketLossPercent = 5 }, simulation);
    }

    [Fact]
    public void ALossOverAHundredPercentIsInvalid() =>
        Assert.Throws<FormatException>(() => OnlineLaunchOptions.Simulation(["--loss", "101"]));
}
