using Scenarios.Tests.Runner;
using Sim.Characters;
using Sim.State;

namespace Scenarios.Tests.Expectations;

/// <summary>What an expectation can look at: the match state before and after the frame, and the setup.</summary>
public sealed class ScenarioFrame
{
    private MatchState _before;
    private MatchState _after;

    public ScenarioFrame(ScenarioSetup setup)
    {
        Setup = setup;
    }

    /// <summary>The frame just run, from 1.</summary>
    public int Number { get; private set; }

    /// <summary>The state the frame started from (what the players saw when they pressed their input).</summary>
    public ref readonly MatchState Before => ref _before;

    /// <summary>The state after the frame; most expectations check this.</summary>
    public ref readonly MatchState After => ref _after;

    public ScenarioSetup Setup { get; }

    public CharacterData Character(int player) => Setup.Character(player);

    internal void Begin(int number, in MatchState state)
    {
        Number = number;
        _before = state;
    }

    internal void End(in MatchState state) => _after = state;
}
