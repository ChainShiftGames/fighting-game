using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 wins</c>: rounds the player has won so far.</summary>
public sealed class WinsKind : ValueKind, ISettableKind
{
    public override string Keyword => "wins";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Round.Wins[player];

    public void Set(ref MatchState state, int player, int value) => state.Round.Wins[player] = value;
}
