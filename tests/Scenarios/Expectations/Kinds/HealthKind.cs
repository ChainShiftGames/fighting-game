using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 health</c>: health left; starts at the character's stats.health.</summary>
public sealed class HealthKind : ValueKind, ISettableKind
{
    public override string Keyword => "health";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Health;

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Health = value;
}
