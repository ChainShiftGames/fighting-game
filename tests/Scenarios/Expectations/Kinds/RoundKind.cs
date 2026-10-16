using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>round</c>: the current round, from 1.</summary>
public sealed class RoundKind : ValueKind, ISettableKind
{
    public override string Keyword => "round";

    public override bool IsPerFighter => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Round.Round;

    public void Set(ref MatchState state, int player, int value) => state.Round.Round = value;
}
