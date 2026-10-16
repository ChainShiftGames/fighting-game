using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Rules;
using Sim.State;

namespace Benchmarks;

/// <summary>
/// The match the benchmark measures: the real game logic, composed from the game data embedded in Sim.Data
/// (ADR-0015) as the game composes it, with the first character in a mirror.
/// </summary>
internal sealed class BenchmarkMatch
{
    // Far enough in that both fighters have moved, attacked and been hit, so states aren't the quiet round start.
    private const int MidMatchFrames = 600;

    private BenchmarkMatch(MatchSimulation simulation, MatchState midMatch)
    {
        Simulation = simulation;
        MidMatch = midMatch;
    }

    public MatchSimulation Simulation { get; }

    /// <summary>A state a few seconds into a round, played from the benchmark's inputs.</summary>
    public MatchState MidMatch { get; }

    public static BenchmarkMatch Create(InputSequence inputs)
    {
        GameData data = new GameDataLoader(new EmbeddedDataSource()).Load();
        CharacterData character = data.Characters[0];
        MatchRules rules = data.CreateMatchRules();
        var simulation = new MatchSimulation(
            character, character, data.Buttons, data.Motions.Motions, data.Motions.Movement, in rules);

        MatchState state = simulation.StartState();
        for (int frame = 0; frame < MidMatchFrames; frame++)
        {
            simulation.Tick(ref state, inputs.Player1At(frame), inputs.Player2At(frame));
        }

        return new BenchmarkMatch(simulation, state);
    }
}
