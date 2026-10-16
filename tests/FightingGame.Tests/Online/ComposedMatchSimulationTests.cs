using System;
using FightingGame.Online;
using Netcode.Drivers;
using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Input;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class ComposedMatchSimulationTests
{
    private const int Seed = 1234;
    private const int Frames = 600;

    private static readonly ushort[] _directions =
    [
        0,
        InputWord.Left,
        InputWord.Right,
        InputWord.Up,
        InputWord.Up | InputWord.Left,
        InputWord.Up | InputWord.Right,
        InputWord.Left | InputWord.Right,
    ];

    private readonly MatchSimulation _match = CreateMatch();

    [Fact]
    public void StartsFromTheComposedStartState()
    {
        using var driver = new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(_match));
        MatchState expected = _match.StartState();

        Assert.Equal(0, driver.CurrentFrame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    [Fact]
    public void RunsTheComposedMatchWithTheGivenInputs()
    {
        using var driver = new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(_match));
        MatchState expected = _match.StartState();
        var random = new Random(Seed);

        for (int frame = 0; frame < Frames; frame++)
        {
            var player1 = new InputWord(_directions[random.Next(_directions.Length)]);
            var player2 = new InputWord(_directions[random.Next(_directions.Length)]);
            Assert.True(driver.TryAdvanceFrame([player1, player2]));
            _match.Tick(ref expected, player1, player2);
        }

        Assert.Equal(Frames, driver.State.Frame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    private static MatchSimulation CreateMatch()
    {
        GameData data = TestGameData.WithFixtureCharacter;
        CharacterData character = TestGameData.FixtureMan;
        MatchRules rules = data.CreateMatchRules();
        return new MatchSimulation(
            character, character, data.Buttons, data.Motions.Motions, data.Motions.Movement, in rules);
    }
}
