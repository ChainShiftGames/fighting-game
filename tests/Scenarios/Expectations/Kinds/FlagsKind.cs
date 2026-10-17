using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 flags</c>: the fighter's flags as one integer, bit i = flag i of data/rules/flags.json (ADR-0023).</summary>
/// <remarks>As data stands: 1 = stance, 2 = install. Settable, so a scenario can start a fighter in a stance.</remarks>
public sealed class FlagsKind : ValueKind, ISettableKind
{
    public override string Keyword => "flags";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Flags;

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Flags = value;
}
