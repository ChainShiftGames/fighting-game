using System;
using FightingGame.Online;
using Netcode.Online;
using Sim.Data.Online;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class OnlineLaunchOptionsTests
{
    private static readonly AutomaticInputDelaySettings _automatic = new(
        MinimumFrames: 1, MaximumFrames: 2, PingStepMilliseconds: 50, PingSampleIntervalFrames: 60, PingSamples: 15);

    [Fact]
    public void CopiesTheAutomaticDelayTuningFromData()
    {
        InputDelaySettings delay = OnlineLaunchOptions.InputDelay(_automatic, playerOverrideFrames: null, []);

        Assert.Equal(
            new InputDelayTuning
            {
                MinimumFrames = 1,
                MaximumFrames = 2,
                PingStepMilliseconds = 50,
                PingSampleIntervalFrames = 60,
                PingSamples = 15,
            },
            delay.Automatic);
        Assert.Null(delay.PlayerOverrideFrames);
    }

    [Fact]
    public void KeepsThePlayersChoice()
    {
        InputDelaySettings delay = OnlineLaunchOptions.InputDelay(_automatic, playerOverrideFrames: 0, ["--online"]);

        Assert.Equal(0, delay.PlayerOverrideFrames);
        Assert.Equal(0, delay.StartingFrames);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void DelayOptionStandsInForThePlayersChoice(int? playerOverrideFrames)
    {
        InputDelaySettings delay = OnlineLaunchOptions.InputDelay(_automatic, playerOverrideFrames, ["--delay", "4"]);

        Assert.Equal(4, delay.PlayerOverrideFrames);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("-1")]
    [InlineData("two")]
    public void RefusesADelayAPlayerCantChoose(string frames)
    {
        Assert.Throws<FormatException>(() => OnlineLaunchOptions.InputDelay(_automatic, null, ["--delay", frames]));
    }
}
