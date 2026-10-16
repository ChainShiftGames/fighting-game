using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 y</c>: height above the floor in subpixels; 0 is grounded.</summary>
public sealed class PositionYKind : ValueKind, ISettableKind
{
    public override string Keyword => "y";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Position.Y;

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Position = state.Fighters[player].Position with { Y = value };
}
