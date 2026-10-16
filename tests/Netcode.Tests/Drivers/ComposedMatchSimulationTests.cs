using System;
using System.IO;
using System.Reflection;
using Netcode.Drivers;
using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Input;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Drivers;

public sealed class ComposedMatchSimulationTests
{
    private const int Seed = 1234;
    private const int Frames = 600;

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
        var inputs = new SeededInputs(Seed);

        for (int frame = 0; frame < Frames; frame++)
        {
            InputWord player1 = inputs.Next();
            InputWord player2 = inputs.Next();
            Assert.True(driver.TryAdvanceFrame([player1, player2]));
            _match.Tick(ref expected, player1, player2);
        }

        Assert.Equal(Frames, driver.State.Frame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    // The Sim.Data fixture is a complete data folder with one character, so this test needs nothing from data/.
    private static MatchSimulation CreateMatch()
    {
        GameData data = new GameDataLoader(new DirectoryDataSource(FixtureDataRoot())).Load();
        CharacterData character = data.Characters[0];
        MatchRules rules = data.CreateMatchRules();
        return new MatchSimulation(
            character, character, data.Buttons, data.Motions.Motions, data.Motions.Movement, in rules);
    }

    private static string FixtureDataRoot()
    {
        foreach (AssemblyMetadataAttribute attribute in
            typeof(ComposedMatchSimulationTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == "RepositoryRoot")
            {
                return Path.Combine(attribute.Value!, "tests", "Sim.Data.Tests", "Fixtures", "data");
            }
        }

        throw new InvalidOperationException("RepositoryRoot metadata is missing from the test assembly.");
    }
}
